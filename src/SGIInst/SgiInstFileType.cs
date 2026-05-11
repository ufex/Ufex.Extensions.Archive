using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.API.Format;
using Ufex.Extensions.Archive.SGIInst.Data;
using Ufex.Extensions.Archive.SGIInst.Structure;

namespace Ufex.Extensions.Archive.SGIInst;

/// <summary>
/// FileType implementation for SGI inst software distribution files (.idb).
/// Parses the IDB manifest and validates associated image files (.sw, .man, etc.).
/// </summary>
public class SgiInstFileType : FileType
{
	public SgiInstFileType()
	{
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override bool ProcessFile()
	{
		var reader = new SgiInstStreamReader(FileInStream, FilePath, Log, ValidationReport);
		bool result = reader.Read();

		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);
		return result;
	}

	private void BuildQuickInfo(SgiInstStreamReader reader)
	{
		QuickInfoTable.AddRow("Format", "SGI inst Distribution");
		QuickInfoTable.AddRow("Base Name", reader.BaseName);
		QuickInfoTable.AddRow("Total Records", reader.Records.Count.ToString());

		int files = reader.Records.Count(r => r.Type == 'f');
		int dirs = reader.Records.Count(r => r.Type == 'd');
		int links = reader.Records.Count(r => r.Type == 'l');
		int other = reader.Records.Count(r => r.Type != 'f' && r.Type != 'd' && r.Type != 'l');

		QuickInfoTable.AddRow("Files", files.ToString());
		QuickInfoTable.AddRow("Directories", dirs.ToString());
		if (links > 0) QuickInfoTable.AddRow("Symbolic Links", links.ToString());
		if (other > 0) QuickInfoTable.AddRow("Other Entries", other.ToString());

		QuickInfoTable.AddRow("Image Files", string.Join(", ", reader.ImageNames.Select(n => $"{reader.BaseName}.{n}")));
		QuickInfoTable.AddRow("Validated Regions", reader.FileRegions.Count.ToString());

		// Subsystem summary
		var subsystems = reader.Records
			.Select(r => r.Subsystem)
			.Distinct()
			.OrderBy(s => s)
			.ToList();
		QuickInfoTable.AddRow("Subsystems", subsystems.Count.ToString());

		// Total size
		long totalSize = reader.Records.Where(r => r.Type == 'f').Sum(r => r.Size);
		QuickInfoTable.AddRow("Total Uncompressed Size", ByteCountFormatter.Format((UInt32)Math.Min(totalSize, UInt32.MaxValue)));

		// Compression stats
		int compressedCount = reader.Records.Count(r => r.Type == 'f' && r.IsCompressed);
		if (compressedCount > 0)
		{
			long totalCompressed = reader.Records.Where(r => r.Type == 'f' && r.IsCompressed).Sum(r => r.CompressedSize);
			QuickInfoTable.AddRow("Compressed Files", compressedCount.ToString());
			QuickInfoTable.AddRow("Total Compressed Size", ByteCountFormatter.Format((UInt32)Math.Min(totalCompressed, UInt32.MaxValue)));
		}

		// Image banners
		foreach (var banner in reader.ImageBanners)
		{
			QuickInfoTable.AddRow($"Banner ({banner.Key})", $"\"{banner.Value.Text}\"");
		}
	}

	private void BuildVisuals(SgiInstStreamReader reader)
	{
		// Build a file map for each image file based on validated regions
		foreach (var imageName in reader.ImageNames)
		{
			var imageRegions = reader.FileRegions
				.Where(r => r.ImageFileName == $"{reader.BaseName}.{imageName}")
				.OrderBy(r => r.StartPosition)
				.ToList();

			if (imageRegions.Count == 0) continue;

			var spans = new List<FileSpan>();

			// Banner span
			if (reader.ImageBanners.TryGetValue(imageName, out var banner))
			{
				spans.Add(new FileSpan
				{
					StartPosition = banner.StartPosition,
					EndPosition = banner.EndPosition,
					Name = $"Banner"
				});
			}

			foreach (var region in imageRegions)
			{
				// Pathname header span
				spans.Add(new FileSpan
				{
					StartPosition = region.StartPosition,
					EndPosition = region.PayloadOffset,
					Name = $"Header: {region.Pathname}"
				});

				// Payload span
				if (region.PayloadLength > 0)
				{
					spans.Add(new FileSpan
					{
						StartPosition = region.PayloadOffset,
						EndPosition = region.EndPosition,
						Name = $"Payload: {region.Pathname}"
					});
				}
			}

			// Estimate total image size from the last region
			ulong imageSize = imageRegions.Count > 0
				? (ulong)imageRegions.Max(r => r.EndPosition)
				: 0;

			var fileMap = new FileMap(spans.ToArray(), imageSize);
			VisualsList.Add(fileMap);
		}
	}

	private void BuildStructure(SgiInstStreamReader reader)
	{
		// Build a lookup of file regions by filename for quick access
		var regionLookup = new Dictionary<string, FileRegion>();
		foreach (var region in reader.FileRegions)
		{
			regionLookup[region.Record.FileName] = region;
		}

		// Group by subsystem
		var subsystemGroups = reader.Records
			.GroupBy(r => r.Subsystem)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in subsystemGroups)
		{
			var subsystemNode = new TreeNode(group.Key, TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);

			// Sub-group by type: directories, files, links, other
			var dirs = group.Where(r => r.Type == 'd').OrderBy(r => r.FileName).ToList();
			var files = group.Where(r => r.Type == 'f').OrderBy(r => r.FileName).ToList();
			var links = group.Where(r => r.Type == 'l').OrderBy(r => r.FileName).ToList();
			var others = group.Where(r => r.Type != 'd' && r.Type != 'f' && r.Type != 'l').OrderBy(r => r.FileName).ToList();

			if (files.Count > 0)
			{
				var filesContainer = new TreeNode($"Files ({files.Count})", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
				foreach (var record in files)
				{
					regionLookup.TryGetValue(record.FileName, out var region);
					filesContainer.Nodes.Add(new IdbRecordNode(record, region));
				}
				subsystemNode.Nodes.Add(filesContainer);
			}

			if (dirs.Count > 0)
			{
				var dirsContainer = new TreeNode($"Directories ({dirs.Count})", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
				foreach (var record in dirs)
					dirsContainer.Nodes.Add(new IdbRecordNode(record, null));
				subsystemNode.Nodes.Add(dirsContainer);
			}

			if (links.Count > 0)
			{
				var linksContainer = new TreeNode($"Links ({links.Count})", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
				foreach (var record in links)
					linksContainer.Nodes.Add(new IdbRecordNode(record, null));
				subsystemNode.Nodes.Add(linksContainer);
			}

			if (others.Count > 0)
			{
				var othersContainer = new TreeNode($"Other ({others.Count})", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
				foreach (var record in others)
					othersContainer.Nodes.Add(new IdbRecordNode(record, null));
				subsystemNode.Nodes.Add(othersContainer);
			}

			TreeNodes.Add(subsystemNode);
		}

		// Add image banners as a separate top-level node
		if (reader.ImageBanners.Count > 0)
		{
			var imagesNode = new TreeNode("Image Files", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
			foreach (var kvp in reader.ImageBanners.OrderBy(b => b.Key))
			{
				var bannerNode = new ImageBannerNode(kvp.Value, $"{reader.BaseName}.{kvp.Key}");
				imagesNode.Nodes.Add(bannerNode);
			}
			TreeNodes.Add(imagesNode);
		}
	}
}
