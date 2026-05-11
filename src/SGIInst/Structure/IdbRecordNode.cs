using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Tables;
using Ufex.API.Visual;
using Ufex.API.Format;
using Ufex.Extensions.Archive.SGIInst.Data;

namespace Ufex.Extensions.Archive.SGIInst.Structure;

/// <summary>
/// Tree node for an IDB record (file, directory, symlink, etc.)
/// Supports extraction for regular file entries.
/// </summary>
internal class IdbRecordNode : TreeNode
{
	public IdbRecord Record { get; }
	public FileRegion? Region { get; }

	public override Visual[] Visuals => [new DataGridVisual(TableData(), "Data")];

	public IdbRecordNode(IdbRecord record, FileRegion? region)
		: base(GetNodeText(record), GetNodeIcon(record), GetNodeIcon(record))
	{
		Record = record;
		Region = region;
	}

	private static string GetNodeText(IdbRecord record)
	{
		return record.FileName.Length > 0 ? record.FileName : "(unnamed)";
	}

	private static TreeViewIcon GetNodeIcon(IdbRecord record)
	{
		return record.Type switch
		{
			'd' => TreeViewIcon.FolderClosed,
			'l' => TreeViewIcon.Object,
			'f' => TreeViewIcon.Document,
			_ => TreeViewIcon.Properties
		};
	}

	public override IReadOnlyList<NodeAction> GetActions()
	{
		if (Record.Type != 'f')
			return [];

		bool hasRegion = Region != null;
		bool isCompressed = Record.IsCompressed;

		string? disabledReason = null;
		if (!hasRegion)
			disabledReason = "Image file not found or region could not be validated";
		else if (isCompressed)
			disabledReason = "Compressed payloads (BSD compress/LZW) are not yet supported for extraction";

		return
		[
			new NodeAction
			{
				Id = "extract",
				Label = "Extract...",
				Group = NodeActionGroup.Export,
				IsEnabled = hasRegion && !isCompressed,
				DisabledReason = disabledReason,
				Inputs =
				[
					new SaveFileInput
					{
						Key = "destination",
						Prompt = "Save extracted file as",
						SuggestedFileName = Path.GetFileName(Record.FileName)
					}
				]
			}
		];
	}

	public override ActionResult ExecuteAction(string actionId, ActionContext context)
	{
		if (actionId != "extract")
			return base.ExecuteAction(actionId, context);

		if (Region == null)
			return ActionResult.Failure("No file region available for extraction");

		var destination = context.GetInput<string>("destination");

		// We need to open the image file independently
		string? idbPath = context.FileContext.FileStream.Name;
		if (string.IsNullOrEmpty(idbPath))
			return ActionResult.Failure("Cannot determine IDB file path");

		string? directory = Path.GetDirectoryName(idbPath);
		if (directory == null)
			return ActionResult.Failure("Cannot determine directory");

		string imagePath = Path.Combine(directory, Region.ImageFileName);
		if (!File.Exists(imagePath))
			return ActionResult.Failure($"Image file not found: {Region.ImageFileName}");

		using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		imageStream.Seek(Region.PayloadOffset, SeekOrigin.Begin);

		using var output = File.Create(destination);
		byte[] buffer = new byte[81920];
		long remaining = Region.PayloadLength;
		while (remaining > 0)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			int toRead = (int)Math.Min(buffer.Length, remaining);
			int read = imageStream.Read(buffer, 0, toRead);
			if (read == 0) break;
			output.Write(buffer, 0, read);
			remaining -= read;
		}

		return ActionResult.Success($"Extracted to {destination}");
	}

	public DynamicTableData TableData()
	{
		var td = new DynamicTableData(3, "SGIInst.IdbRecordProperties");
		td.SetColumn(0, "Property");
		td.SetColumn(1, "Value");
		td.SetColumn(2, "Description");

		td.AddRow("Type", Record.Type.ToString(), Record.TypeDescription);
		td.AddRow("Mode", Record.ModeString, Record.ModeDescription);
		td.AddRow("Owner", Record.Owner, "");
		td.AddRow("Group", Record.Group, "");
		td.AddRow("Path", Record.FileName, "");
		td.AddRow("Source", Record.Source, "");
		td.AddRow("Subsystem", Record.Subsystem, "");

		if (Record.Type == 'f')
		{
			td.AddRow("Image", Record.ImageName, "");
			td.AddRow("Offset", Record.Offset.ToString(), $"0x{Record.Offset:X}");
			td.AddRow("Size", Record.Size.ToString(), ByteCountFormatter.Format((UInt32)Math.Min(Record.Size, UInt32.MaxValue)));
			td.AddRow("Compressed Size", Record.CompressedSize.ToString(),
				Record.IsCompressed
					? ByteCountFormatter.Format((UInt32)Math.Min(Record.CompressedSize, UInt32.MaxValue))
					: "Uncompressed");
			if (Record.Checksum > 0)
				td.AddRow("Checksum (sum)", Record.Checksum.ToString(), "");
		}

		if (Record.Type == 'l' && Record.SymlinkValue != null)
		{
			td.AddRow("Symlink Target", Record.SymlinkValue, "");
		}

		if (Record.MachPredicate != null)
		{
			td.AddRow("Architecture", Record.MachPredicate, "");
		}

		if (Record.IsConfig)
		{
			td.AddRow("Config", "Yes", "User-modifiable configuration file");
		}

		// Show any additional attributes not covered above
		var knownAttrs = new HashSet<string> { "off", "size", "cmpsize", "sum", "symval", "mach", "config", "f" };
		foreach (var attr in Record.Attributes)
		{
			if (knownAttrs.Contains(attr.Key)) continue;
			td.AddRow(attr.Key, attr.Value, "");
		}

		if (Region != null)
		{
			td.AddRow("--- Image Region ---", "", "");
			td.AddRow("Embedded Pathname", Region.Pathname, Region.PathnameMatches ? "Matches IDB" : "MISMATCH");
			td.AddRow("Pathname Length", Region.PathnameLength.ToString(), "");
			td.AddRow("Payload Offset", Region.PayloadOffset.ToString(), $"0x{Region.PayloadOffset:X}");
			td.AddRow("Payload Length", Region.PayloadLength.ToString(),
				ByteCountFormatter.Format((UInt32)Math.Min(Region.PayloadLength, UInt32.MaxValue)));
		}

		return td;
	}
}
