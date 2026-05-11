using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// CAB Descriptor node
/// </summary>
internal class CabDescriptorNode : SectionNode
{
	public override string Description => "CAB Descriptor";

	public CabDescriptorNode(CabDescriptor descriptor)
		: base(descriptor, "CAB Descriptor", TreeViewIcon.Properties)
	{
	}

	public override object[][] GetRows()
	{
		var d = (CabDescriptor)Section;
		return [
			["Unknown (12 bytes)", d.Unknown1, $"{d.Unknown1.Length} bytes"],
			["File Table Offset", d.FileTableOffset, $"0x{d.FileTableOffset:X8}"],
			["Unknown (4 bytes)", d.Unknown2, $"{d.Unknown2.Length} bytes"],
			["File Table Size", d.FileTableSize, ByteCountFormatter.Format(d.FileTableSize)],
			["File Table Size 2", d.FileTableSize2, ByteCountFormatter.Format(d.FileTableSize2)],
			["Directory Count", d.DirectoryCount],
			["Unknown (8 bytes)", d.Unknown3, $"{d.Unknown3.Length} bytes"],
			["File Count", d.FileCount],
			["File Table Offset 2", d.FileTableOffset2, $"0x{d.FileTableOffset2:X8}"],
			["Unknown (14 bytes)", d.Unknown4, $"{d.Unknown4.Length} bytes"],
		];
	}
}
