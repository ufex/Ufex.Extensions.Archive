using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.API.Format;
using Ufex.Extensions.Archive.ISCAB.Data;
using Ufex.Extensions.Archive.ISCAB.Structure;

namespace Ufex.Extensions.Archive.ISCAB;

/// <summary>
/// FileType implementation for InstallShield Cabinet (ISCAB) files.
/// Handles both .cab and .hdr files; if a .hdr companion is found alongside
/// the opened .cab file, it is loaded automatically for metadata.
/// </summary>
public class IscabFileType : FileType
{
	public IscabFileType()
	{
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override bool ProcessFile()
	{
		// Try to find companion .hdr file
		Stream? hdrStream = FindHdrFile();

		var reader = new IscabStreamReader(FileInStream, hdrStream, Log, ValidationReport);
		bool result = reader.Read();

		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);

		// Clean up hdr stream if we opened one
		hdrStream?.Dispose();

		return result;
	}

	private Stream? FindHdrFile()
	{
		if (string.IsNullOrEmpty(FilePath))
			return null;

		string? directory = Path.GetDirectoryName(FilePath);
		if (directory == null)
			return null;

		string fileName = Path.GetFileNameWithoutExtension(FilePath);
		string extension = Path.GetExtension(FilePath).ToLowerInvariant();

		// If the user opened the .hdr file itself, look for the .cab
		// If the user opened the .cab file, look for the .hdr
		string hdrPath;
		if (extension == ".hdr")
		{
			// User opened the HDR directly - no companion needed, it IS the metadata
			return null;
		}
		else
		{
			// User opened a .cab - look for matching .hdr
			hdrPath = Path.Combine(directory, fileName + ".hdr");
		}

		// Try case-insensitive search
		if (File.Exists(hdrPath))
		{
			Log.LogInformation($"Found companion HDR file: {hdrPath}");
			return new FileStream(hdrPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		// Try alternate casing
		string hdrPathUpper = Path.Combine(directory, fileName + ".HDR");
		if (File.Exists(hdrPathUpper))
		{
			Log.LogInformation($"Found companion HDR file: {hdrPathUpper}");
			return new FileStream(hdrPathUpper, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		Log.LogInformation("No companion HDR file found");
		return null;
	}

	private void BuildQuickInfo(IscabStreamReader reader)
	{
		QuickInfoTable.AddRow("Format", "InstallShield Cabinet (ISCAB)");
		QuickInfoTable.AddRow("Major Version", reader.MajorVersion.ToString());
		QuickInfoTable.AddRow("Raw Version", $"0x{reader.CabCommonHeader!.Version:X8}");
		QuickInfoTable.AddRow("HDR File", reader.HasHdrFile ? "Present" : "Not found");

		if (reader.Descriptor != null)
		{
			QuickInfoTable.AddRow("Directories", reader.Descriptor.DirectoryCount.ToString());
			QuickInfoTable.AddRow("Files", reader.Descriptor.FileCount.ToString());
		}

		QuickInfoTable.AddRow("File Groups", reader.FileGroups.Count.ToString());
		QuickInfoTable.AddRow("Components", reader.Components.Count.ToString());

		if (reader.MajorVersion >= 6)
		{
			int compressed = reader.FilesV6.Count(f => (f.Flags & Signatures.FileCompressed) != 0);
			QuickInfoTable.AddRow("Compressed Files", compressed.ToString());
		}
		else
		{
			int compressed = reader.FilesV5.Count(f => (f.Flags & Signatures.FileCompressed) != 0);
			QuickInfoTable.AddRow("Compressed Files", compressed.ToString());
		}
	}

	private void BuildVisuals(IscabStreamReader reader)
	{
		// Build file map for the cab file
		var spans = new List<FileSpan>();

		// Common header
		spans.Add(new FileSpan
		{
			StartPosition = reader.CabCommonHeader!.StartPosition,
			EndPosition = reader.CabCommonHeader.EndPosition,
			Name = "Common Header"
		});

		// Volume header
		if (reader.CabVolumeHeader != null)
		{
			spans.Add(new FileSpan
			{
				StartPosition = reader.CabVolumeHeader.StartPosition,
				EndPosition = reader.CabVolumeHeader.EndPosition,
				Name = "Volume Header"
			});
		}

		var fileMap = new FileMap(spans.ToArray(), (ulong)FileInStream.Length);
		VisualsList.Add(fileMap);
	}

	private void BuildStructure(IscabStreamReader reader)
	{
		// HDR section (if present)
		if (reader.HasHdrFile)
		{
			var hdrRoot = new TreeNode("HDR File", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);

			// HDR Common Header
			hdrRoot.Nodes.Add(new CommonHeaderNode(reader.HdrCommonHeader!, "HDR"));

			// CAB Descriptor
			if (reader.Descriptor != null)
			{
				hdrRoot.Nodes.Add(new CabDescriptorNode(reader.Descriptor));
			}

			// Directories
			var dirsNode = new TreeNode("Directories", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			for (int i = 0; i < reader.Directories.Count; i++)
			{
				dirsNode.Nodes.Add(new TreeNode($"[{i}] {reader.Directories[i]}", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen));
			}
			hdrRoot.Nodes.Add(dirsNode);

			// Files
			var filesNode = new TreeNode("Files", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			if (reader.MajorVersion >= 6)
			{
				foreach (var fd in reader.FilesV6)
				{
					filesNode.Nodes.Add(new FileDescriptorV6Node(fd));
				}
			}
			else
			{
				foreach (var fd in reader.FilesV5)
				{
					filesNode.Nodes.Add(new FileDescriptorV5Node(fd));
				}
			}
			hdrRoot.Nodes.Add(filesNode);

			// File Groups
			var fgNode = new TreeNode("File Groups", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			foreach (var fg in reader.FileGroups)
			{
				fgNode.Nodes.Add(new FileGroupNode(fg));
			}
			hdrRoot.Nodes.Add(fgNode);

			// Components
			var compNode = new TreeNode("Components", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			foreach (var comp in reader.Components)
			{
				compNode.Nodes.Add(new ComponentNode(comp));
			}
			hdrRoot.Nodes.Add(compNode);

			TreeNodes.Add(hdrRoot);
		}

		// CAB section
		var cabRoot = new TreeNode("CAB File", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);

		// CAB Common Header
		cabRoot.Nodes.Add(new CommonHeaderNode(reader.CabCommonHeader!, "CAB"));

		// Volume Header
		if (reader.CabVolumeHeader != null)
		{
			cabRoot.Nodes.Add(new VolumeHeaderNode(reader.CabVolumeHeader));
		}

		// If no HDR file, metadata is in the CAB - show it here
		if (!reader.HasHdrFile)
		{
			if (reader.Descriptor != null)
			{
				cabRoot.Nodes.Add(new CabDescriptorNode(reader.Descriptor));
			}

			// Directories
			var dirsNode = new TreeNode("Directories", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			for (int i = 0; i < reader.Directories.Count; i++)
			{
				dirsNode.Nodes.Add(new TreeNode($"[{i}] {reader.Directories[i]}", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen));
			}
			cabRoot.Nodes.Add(dirsNode);

			// Files
			var filesNode = new TreeNode("Files", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			if (reader.MajorVersion >= 6)
			{
				foreach (var fd in reader.FilesV6)
				{
					filesNode.Nodes.Add(new FileDescriptorV6Node(fd));
				}
			}
			else
			{
				foreach (var fd in reader.FilesV5)
				{
					filesNode.Nodes.Add(new FileDescriptorV5Node(fd));
				}
			}
			cabRoot.Nodes.Add(filesNode);

			// File Groups
			var fgNode = new TreeNode("File Groups", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			foreach (var fg in reader.FileGroups)
			{
				fgNode.Nodes.Add(new FileGroupNode(fg));
			}
			cabRoot.Nodes.Add(fgNode);

			// Components
			var compNode = new TreeNode("Components", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			foreach (var comp in reader.Components)
			{
				compNode.Nodes.Add(new ComponentNode(comp));
			}
			cabRoot.Nodes.Add(compNode);
		}

		TreeNodes.Add(cabRoot);
	}
}
