using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.API.Format;
using Ufex.Extensions.Archive.TAR.Data;
using Ufex.Extensions.Archive.TAR.Structure;

namespace Ufex.Extensions.Archive.TAR;

/// <summary>
/// FileType implementation for TAR archive files.
/// Supports POSIX ustar, GNU tar, and V7 (old-style) formats.
/// </summary>
public class TarFileType : FileType
{
	public TarFileType()
	{
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override bool ProcessFile()
	{
		var reader = new TarStreamReader(FileInStream, Log, ValidationReport);
		bool result = reader.Read();

		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);
		return result;
	}

	private void BuildQuickInfo(TarStreamReader reader)
	{
		QuickInfoTable.AddRow("Format", reader.Format);
		QuickInfoTable.AddRow("Entries", reader.Entries.Count.ToString());

		int files = reader.Entries.Count(e => e.Header.TypeFlagChar == '\0' || e.Header.TypeFlagChar == '0');
		int dirs = reader.Entries.Count(e => e.Header.TypeFlagChar == '5');
		int links = reader.Entries.Count(e => e.Header.TypeFlagChar == '1' || e.Header.TypeFlagChar == '2');

		QuickInfoTable.AddRow("Files", files.ToString());
		QuickInfoTable.AddRow("Directories", dirs.ToString());
		if (links > 0)
			QuickInfoTable.AddRow("Links", links.ToString());

		UInt64 totalSize = 0;
		foreach (var entry in reader.Entries)
		{
			totalSize += entry.Header.Size;
		}
		QuickInfoTable.AddRow("Total Uncompressed Size", ByteCountFormatter.Format((UInt32)Math.Min(totalSize, UInt32.MaxValue)));
	}

	private void BuildVisuals(TarStreamReader reader)
	{
		var spans = new List<FileSpan>();
		foreach (var entry in reader.Entries)
		{
			// Header span
			spans.Add(new FileSpan
			{
				StartPosition = entry.Header.StartPosition,
				EndPosition = entry.Header.EndPosition,
				Name = $"Header: {entry.DisplayName}"
			});

			// Data span (if any)
			if (entry.Header.Size > 0)
			{
				spans.Add(new FileSpan
				{
					StartPosition = entry.Header.DataOffset,
					EndPosition = entry.Header.DataEndPosition,
					Name = $"Data: {entry.DisplayName}"
				});
			}
		}

		var fileMap = new FileMap(spans.ToArray(), (ulong)FileInStream.Length);
		VisualsList.Add(fileMap);
	}

	private void BuildStructure(TarStreamReader reader)
	{
		var filesNode = new TreeNode("Files", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		var dirsNode = new TreeNode("Directories", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		var otherNode = new TreeNode("Other", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);

		foreach (var entry in reader.Entries)
		{
			var node = new TarEntryNode(entry);
			switch (entry.Header.TypeFlagChar)
			{
				case '\0' or '0' or '7':
					filesNode.Nodes.Add(node);
					break;
				case '5':
					dirsNode.Nodes.Add(node);
					break;
				default:
					otherNode.Nodes.Add(node);
					break;
			}
		}

		if (filesNode.Nodes.Count > 0) TreeNodes.Add(filesNode);
		if (dirsNode.Nodes.Count > 0) TreeNodes.Add(dirsNode);
		if (otherNode.Nodes.Count > 0) TreeNodes.Add(otherNode);
	}
}
