using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Tables;
using Ufex.API.Visual;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB.Structure;

/// <summary>
/// Base TreeNode class for CAB file sections
/// </summary>
internal class SectionNode : TreeNode
{
	public Section Section { get; }

	public virtual string Description => "Section";

	public override Visual[] Visuals => [new DataGridVisual(TableData(), "Data")];

	protected SectionNode(Section section, string text, TreeViewIcon icon)
		: base(text, icon, icon)
	{
		Section = section;
	}

	public virtual DynamicTableData TableData()
	{
		var td = new DynamicTableData(4, "CAB.PropertyValueDescription");
		td.SetColumn(0, "Property");
		td.SetColumn(1, "Value");
		td.SetColumn(2, "Description");
		td.SetColumn(3, "Offset");

		var rows = GetRows();
		long offset = Section.StartPosition;
		for (int i = 0; i < rows.Length; i++)
		{
			td.AddRow(rows[i][0], rows[i][1], rows[i].Length > 2 ? rows[i][2] : "", new FileOffset(offset));
			offset += ByteUtil.GetObjectSize(rows[i][1]);
		}
		return td;
	}

	public virtual object[][] GetRows() => [];
}
