using System.IO.Compression;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB.Structure;

/// <summary>
/// CFFILE - Cabinet file entry node
/// </summary>
internal class CfFileNode : SectionNode
{
	private readonly CfFolder? _folder;
	private readonly List<CfData>? _dataBlocks;

	public override string Description => $"File: {((CfFile)Section).SzName}";

	public CfFileNode(CfFile file)
		: base(file, file.SzName, TreeViewIcon.Document)
	{
	}

	public CfFileNode(CfFile file, CfFolder folder, List<CfData> dataBlocks)
		: base(file, file.SzName, TreeViewIcon.Document)
	{
		_folder = folder;
		_dataBlocks = dataBlocks;
	}

	public override IReadOnlyList<NodeAction> GetActions()
	{
		var file = (CfFile)Section;
		bool hasFolder = _folder != null && _dataBlocks != null;
		bool isSpecialFolder = file.IFolder >= Signatures.IfoldContinuedFromPrev;

		// Only support extraction for MSZIP and uncompressed folders
		UInt16 compType = hasFolder ? (UInt16)(_folder!.TypeCompress & 0x000F) : (UInt16)0xFFFF;
		bool supportedCompression = compType == 0x0000 || compType == 0x0001; // None or MSZIP

		bool canExtract = hasFolder && !isSpecialFolder && supportedCompression && file.CbFile > 0;

		string? disabledReason = null;
		if (!hasFolder || isSpecialFolder)
			disabledReason = "File spans multiple cabinets";
		else if (!supportedCompression)
			disabledReason = $"Unsupported compression: {_folder!.TypeCompressText}";
		else if (file.CbFile == 0)
			disabledReason = "File is empty";

		return
		[
			new NodeAction
			{
				Id = "extract",
				Label = "Extract...",
				Group = NodeActionGroup.Export,
				IsEnabled = canExtract,
				DisabledReason = disabledReason,
				Inputs =
				[
					new SaveFileInput
					{
						Key = "destination",
						Prompt = "Save extracted file as",
						SuggestedFileName = Path.GetFileName(file.SzName)
					}
				]
			}
		];
	}

	public override ActionResult ExecuteAction(string actionId, ActionContext context)
	{
		if (actionId != "extract")
			return base.ExecuteAction(actionId, context);

		var file = (CfFile)Section;
		var destination = context.GetInput<string>("destination");
		var stream = context.FileContext.FileStream;

		// Decompress all data blocks for this folder, then extract the file's range
		UInt16 compType = (UInt16)(_folder!.TypeCompress & 0x000F);

		using var output = File.Create(destination);
		using var folderData = new MemoryStream();

		foreach (var block in _dataBlocks!)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			stream.Seek(block.DataOffset, SeekOrigin.Begin);
			byte[] compressedPayload = new byte[block.CbData];
			stream.ReadExactly(compressedPayload);

			if (compType == 0x0000) // None
			{
				folderData.Write(compressedPayload);
			}
			else if (compType == 0x0001) // MSZIP
			{
				// MSZIP blocks start with 'CK' signature (0x43, 0x4B)
				if (compressedPayload.Length >= 2 && compressedPayload[0] == 0x43 && compressedPayload[1] == 0x4B)
				{
					using var compStream = new MemoryStream(compressedPayload, 2, compressedPayload.Length - 2);
					using var deflate = new DeflateStream(compStream, CompressionMode.Decompress);
					deflate.CopyTo(folderData);
				}
				else
				{
					return ActionResult.Failure("Invalid MSZIP block: missing CK signature");
				}
			}

			// Check if we have enough data already
			if (folderData.Length >= file.UoffFolderStart + file.CbFile)
				break;
		}

		// Extract the file's byte range from the decompressed folder data
		if (folderData.Length < file.UoffFolderStart + file.CbFile)
		{
			return ActionResult.Failure("Insufficient data: file may span multiple cabinets");
		}

		folderData.Seek(file.UoffFolderStart, SeekOrigin.Begin);
		byte[] buffer = new byte[81920];
		long remaining = file.CbFile;
		while (remaining > 0)
		{
			int toRead = (int)Math.Min(buffer.Length, remaining);
			int read = folderData.Read(buffer, 0, toRead);
			if (read == 0) break;
			output.Write(buffer, 0, read);
			remaining -= read;
		}

		return ActionResult.Success($"Extracted to {destination}");
	}

	public override object[][] GetRows()
	{
		var d = (CfFile)Section;
		return [
			["Uncompressed Size", d.CbFile, ByteCountFormatter.Format(d.CbFile)],
			["Folder Offset", d.UoffFolderStart, $"0x{d.UoffFolderStart:X8}"],
			["Folder Index", d.IFolder, Constants.FolderIndexDescription(d.IFolder, 0)],
			["Date", d.Date, d.DateText],
			["Time", d.Time, d.TimeText],
			["Attributes", d.Attribs, d.AttribsText],
			["File Name", d.SzNameBytes, d.SzName],
		];
	}
}
