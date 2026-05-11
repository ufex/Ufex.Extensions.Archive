using Ufex.API;
using Ufex.API.Tree;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// File group descriptor node
/// </summary>
internal class FileGroupNode : SectionNode
{
	public override string Description => $"File Group: {((FileGroupDescriptor)Section).Name}";

	public FileGroupNode(FileGroupDescriptor fg)
		: base(fg, fg.Name.Length > 0 ? fg.Name : "(unnamed)", TreeViewIcon.FolderClosed)
	{
	}

	public override object[][] GetRows()
	{
		var d = (FileGroupDescriptor)Section;
		return [
			["Name Offset", d.NameOffset, d.Name],
			["Unknown", d.Unknown, $"{d.Unknown.Length} bytes"],
			["First File", (UInt32)(d.FirstFile >= 0 ? d.FirstFile : 0), d.FirstFile >= 0 ? d.FirstFile.ToString() : "(empty)"],
			["Last File", (UInt32)(d.LastFile >= 0 ? d.LastFile : 0), d.LastFile >= 0 ? d.LastFile.ToString() : "(empty)"],
		];
	}
}
