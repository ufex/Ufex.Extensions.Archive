using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB.Structure;

/// <summary>
/// CFFOLDER - Cabinet folder node
/// </summary>
internal class CfFolderNode : SectionNode
{
	private int _index;

	public override string Description => $"Folder {_index} ({((CfFolder)Section).TypeCompressText})";

	public CfFolderNode(CfFolder folder, int index)
		: base(folder, $"CFFOLDER {index}", TreeViewIcon.FolderClosed)
	{
		_index = index;
	}

	public override object[][] GetRows()
	{
		var d = (CfFolder)Section;
		var rows = new List<object[]>();
		rows.Add(["Data Offset", d.CoffCabStart, $"0x{d.CoffCabStart:X8}"]);
		rows.Add(["Data Block Count", d.CCFData]);
		rows.Add(["Compression Type", d.TypeCompress, d.TypeCompressText]);

		if (d.AbReserve.Length > 0)
		{
			rows.Add(["Reserve Data", d.AbReserve, $"{d.AbReserve.Length} bytes"]);
		}

		return rows.ToArray();
	}
}
