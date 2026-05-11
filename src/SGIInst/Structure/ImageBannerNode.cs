using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Tables;
using Ufex.API.Visual;
using Ufex.Extensions.Archive.SGIInst.Data;

namespace Ufex.Extensions.Archive.SGIInst.Structure;

/// <summary>
/// Tree node for an image file banner
/// </summary>
internal class ImageBannerNode : SectionNode
{
	private readonly ImageBanner _banner;
	private readonly string _imageName;

	public ImageBannerNode(ImageBanner banner, string imageName)
		: base(banner, $"Banner: \"{banner.Text}\"", TreeViewIcon.Properties)
	{
		_banner = banner;
		_imageName = imageName;
	}

	public override object[][] GetRows()
	{
		var rows = new List<object[]>();
		rows.Add(["Image", _imageName, ""]);
		rows.Add(["Banner Text", _banner.Text, ""]);
		rows.Add(["Banner Bytes", _banner.RawBytes, $"{_banner.RawBytes.Length} bytes (including NUL)"]);
		return rows.ToArray();
	}
}
