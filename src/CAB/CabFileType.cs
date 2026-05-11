using System.Linq;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Archive.CAB.Data;
using Ufex.Extensions.Archive.CAB.Structure;

namespace Ufex.Extensions.Archive.CAB;

/// <summary>
/// FileType implementation for Microsoft Cabinet (CAB) files.
/// </summary>
public class CabFileType : FileType
{
	public CabFileType()
	{
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override bool ProcessFile()
	{
		var reader = new CabStreamReader(FileInStream, Log, ValidationReport);
		bool result = reader.Read();

		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);
		return result;
	}

	private void BuildQuickInfo(CabStreamReader reader)
	{
		var header = reader.Header!;
		QuickInfoTable.AddRow("Version", header.VersionText);
		QuickInfoTable.AddRow("Cabinet Size", Ufex.API.Format.ByteCountFormatter.Format(header.CbCabinet));
		QuickInfoTable.AddRow("Folders", header.CFolders.ToString());
		QuickInfoTable.AddRow("Files", header.CFiles.ToString());
		QuickInfoTable.AddRow("Set ID", header.SetID.ToString());
		QuickInfoTable.AddRow("Cabinet Index", header.ICabinet.ToString());

		var compressionTypes = reader.Folders
			.Select(f => f.TypeCompressText)
			.Distinct();
		QuickInfoTable.AddRow("Compression", string.Join(", ", compressionTypes));

		if (header.HasPrevCabinet)
			QuickInfoTable.AddRow("Previous Cabinet", header.SzCabinetPrev);
		if (header.HasNextCabinet)
			QuickInfoTable.AddRow("Next Cabinet", header.SzCabinetNext);
	}

	private void BuildVisuals(CabStreamReader reader)
	{
		var spans = new List<FileSpan>();

		// Header span
		spans.Add(new FileSpan
		{
			StartPosition = reader.Header!.StartPosition,
			EndPosition = reader.Header.EndPosition,
			Name = "CFHEADER"
		});

		// Folder entry spans
		foreach (var folder in reader.Folders)
		{
			spans.Add(new FileSpan
			{
				StartPosition = folder.StartPosition,
				EndPosition = folder.EndPosition,
				Name = "CFFOLDER"
			});
		}

		// File entry spans
		foreach (var file in reader.Files)
		{
			spans.Add(new FileSpan
			{
				StartPosition = file.StartPosition,
				EndPosition = file.EndPosition,
				Name = $"CFFILE ({file.SzName})"
			});
		}

		// Data block spans
		foreach (var data in reader.DataBlocks)
		{
			spans.Add(new FileSpan
			{
				StartPosition = data.StartPosition,
				EndPosition = data.EndPosition,
				Name = "CFDATA"
			});
		}

		var fileMap = new FileMap(spans.ToArray(), (ulong)FileInStream.Length);
		VisualsList.Add(fileMap);
	}

	private void BuildStructure(CabStreamReader reader)
	{
		// Header node
		var headerNode = new CfHeaderNode(reader.Header!);
		TreeNodes.Add(headerNode);

		// Folders node
		var foldersNode = new TreeNode("Folders", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		for (int i = 0; i < reader.Folders.Count; i++)
		{
			var folderNode = new CfFolderNode(reader.Folders[i], i);
			foldersNode.Nodes.Add(folderNode);
		}
		TreeNodes.Add(foldersNode);

		// Build a mapping of folder index to its data blocks
		var folderDataBlocks = new Dictionary<int, List<CfData>>();
		int blockIndex = 0;
		for (int i = 0; i < reader.Folders.Count; i++)
		{
			var blocks = new List<CfData>();
			for (int j = 0; j < reader.Folders[i].CCFData && blockIndex < reader.DataBlocks.Count; j++)
			{
				blocks.Add(reader.DataBlocks[blockIndex++]);
			}
			folderDataBlocks[i] = blocks;
		}

		// Files node
		var filesNode = new TreeNode("Files", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		foreach (var file in reader.Files)
		{
			CfFileNode fileNode;
			if (file.IFolder < reader.Folders.Count && folderDataBlocks.ContainsKey(file.IFolder))
			{
				fileNode = new CfFileNode(file, reader.Folders[file.IFolder], folderDataBlocks[file.IFolder]);
			}
			else
			{
				fileNode = new CfFileNode(file);
			}
			filesNode.Nodes.Add(fileNode);
		}
		TreeNodes.Add(filesNode);

		// Data blocks node
		var dataNode = new TreeNode("Data Blocks", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		for (int i = 0; i < reader.DataBlocks.Count; i++)
		{
			var blockNode = new CfDataNode(reader.DataBlocks[i], i);
			dataNode.Nodes.Add(blockNode);
		}
		TreeNodes.Add(dataNode);
	}
}
