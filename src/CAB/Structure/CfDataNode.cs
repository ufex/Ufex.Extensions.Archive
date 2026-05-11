using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB.Structure;

/// <summary>
/// CFDATA - Cabinet data block node
/// </summary>
internal class CfDataNode : SectionNode
{
	private int _index;

	public override string Description => $"Data Block {_index}";

	public CfDataNode(CfData data, int index)
		: base(data, $"CFDATA {index}", TreeViewIcon.Binary)
	{
		_index = index;
	}

	public override object[][] GetRows()
	{
		var d = (CfData)Section;
		var rows = new List<object[]>();
		rows.Add(["Checksum", d.Checksum, $"0x{d.Checksum:X8}"]);
		rows.Add(["Compressed Size", d.CbData, ByteCountFormatter.Format(d.CbData)]);
		rows.Add(["Uncompressed Size", d.CbUncomp, ByteCountFormatter.Format(d.CbUncomp)]);

		if (d.AbReserve.Length > 0)
		{
			rows.Add(["Reserve Data", d.AbReserve, $"{d.AbReserve.Length} bytes"]);
		}

		return rows.ToArray();
	}
}
