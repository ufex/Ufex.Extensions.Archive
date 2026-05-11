using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// Common Header node (20 bytes)
/// </summary>
internal class CommonHeaderNode : SectionNode
{
	private string _source;

	public override string Description => $"Common Header ({_source})";

	public CommonHeaderNode(CommonHeader header, string source)
		: base(header, $"Common Header", TreeViewIcon.Header)
	{
		_source = source;
	}

	public override object[][] GetRows()
	{
		var d = (CommonHeader)Section;
		return [
			["Signature", d.Signature, $"0x{d.Signature:X8} (ISc()"],
			["Version", d.Version, $"0x{d.Version:X8} (Major: {d.MajorVersion})"],
			["Volume Info", d.VolumeInfo, $"0x{d.VolumeInfo:X8}"],
			["Descriptor Offset", d.CabDescriptorOffset, $"0x{d.CabDescriptorOffset:X8}"],
			["Descriptor Size", d.CabDescriptorSize, ByteCountFormatter.Format(d.CabDescriptorSize)],
		];
	}
}
