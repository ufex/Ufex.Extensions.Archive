using Ufex.API;
using Ufex.API.Tree;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// Component descriptor node
/// </summary>
internal class ComponentNode : SectionNode
{
	public override string Description => $"Component: {((ComponentDescriptor)Section).Name}";

	public ComponentNode(ComponentDescriptor comp)
		: base(comp, comp.Name.Length > 0 ? comp.Name : "(unnamed)", TreeViewIcon.Gear)
	{
	}

	public override object[][] GetRows()
	{
		var d = (ComponentDescriptor)Section;
		return [
			["Name Offset", d.NameOffset, d.Name],
			["Unknown", d.Unknown, $"{d.Unknown.Length} bytes"],
			["File Group Count", d.FileGroupCount],
			["File Group Table Offset", d.FileGroupTableOffset, $"0x{d.FileGroupTableOffset:X8}"],
		];
	}
}
