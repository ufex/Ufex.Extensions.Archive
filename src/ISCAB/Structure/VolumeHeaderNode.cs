using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// Volume Header node (cab files only)
/// </summary>
internal class VolumeHeaderNode : SectionNode
{
	public override string Description => "Volume Header";

	public VolumeHeaderNode(VolumeHeader header)
		: base(header, "Volume Header", TreeViewIcon.Properties)
	{
	}

	public override object[][] GetRows()
	{
		var d = (VolumeHeader)Section;
		var rows = new List<object[]>();

		if (d.IsV6)
		{
			rows.Add(["Data Offset", d.DataOffset, $"0x{d.DataOffset64:X16}"]);
			rows.Add(["Data Offset High", d.DataOffsetHigh]);
			rows.Add(["First File Index", d.FirstFileIndex]);
			rows.Add(["Last File Index", d.LastFileIndex]);
			rows.Add(["First File Offset", d.FirstFileOffset, $"0x{d.FirstFileOffset64:X16}"]);
			rows.Add(["First File Offset High", d.FirstFileOffsetHigh]);
			rows.Add(["First File Size Expanded", d.FirstFileSizeExpanded, ByteCountFormatter.Format(d.FirstFileSizeExpanded)]);
			rows.Add(["First File Size Expanded High", d.FirstFileSizeExpandedHigh]);
			rows.Add(["First File Size Compressed", d.FirstFileSizeCompressed, ByteCountFormatter.Format(d.FirstFileSizeCompressed)]);
			rows.Add(["First File Size Compressed High", d.FirstFileSizeCompressedHigh]);
			rows.Add(["Last File Offset", d.LastFileOffset, $"0x{d.LastFileOffset64:X16}"]);
			rows.Add(["Last File Offset High", d.LastFileOffsetHigh]);
			rows.Add(["Last File Size Expanded", d.LastFileSizeExpanded, ByteCountFormatter.Format(d.LastFileSizeExpanded)]);
			rows.Add(["Last File Size Expanded High", d.LastFileSizeExpandedHigh]);
			rows.Add(["Last File Size Compressed", d.LastFileSizeCompressed, ByteCountFormatter.Format(d.LastFileSizeCompressed)]);
			rows.Add(["Last File Size Compressed High", d.LastFileSizeCompressedHigh]);
		}
		else
		{
			rows.Add(["Data Offset", d.DataOffset, $"0x{d.DataOffset:X8}"]);
			rows.Add(["First File Index", d.FirstFileIndex]);
			rows.Add(["Last File Index", d.LastFileIndex]);
			rows.Add(["First File Offset", d.FirstFileOffset, $"0x{d.FirstFileOffset:X8}"]);
			rows.Add(["First File Size Expanded", d.FirstFileSizeExpanded, ByteCountFormatter.Format(d.FirstFileSizeExpanded)]);
			rows.Add(["First File Size Compressed", d.FirstFileSizeCompressed, ByteCountFormatter.Format(d.FirstFileSizeCompressed)]);
			rows.Add(["Last File Offset", d.LastFileOffset, $"0x{d.LastFileOffset:X8}"]);
			rows.Add(["Last File Size Expanded", d.LastFileSizeExpanded, ByteCountFormatter.Format(d.LastFileSizeExpanded)]);
			rows.Add(["Last File Size Compressed", d.LastFileSizeCompressed, ByteCountFormatter.Format(d.LastFileSizeCompressed)]);
		}

		return rows.ToArray();
	}
}
