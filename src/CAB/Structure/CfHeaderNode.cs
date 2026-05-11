using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB.Structure;

/// <summary>
/// CFHEADER - Cabinet header node
/// </summary>
internal class CfHeaderNode : SectionNode
{
	public override string Description => "Cabinet Header (CFHEADER)";

	public CfHeaderNode(CfHeader header)
		: base(header, "CFHEADER", TreeViewIcon.Header)
	{
	}

	public override object[][] GetRows()
	{
		var d = (CfHeader)Section;
		var rows = new List<object[]>();
		rows.Add(["Signature", d.Signature, "MSCF"]);
		rows.Add(["Reserved1", d.Reserved1]);
		rows.Add(["Cabinet Size", d.CbCabinet, ByteCountFormatter.Format(d.CbCabinet)]);
		rows.Add(["Reserved2", d.Reserved2]);
		rows.Add(["File Entries Offset", d.CoffFiles, $"0x{d.CoffFiles:X8}"]);
		rows.Add(["Reserved3", d.Reserved3]);
		rows.Add(["Version Minor", d.VersionMinor]);
		rows.Add(["Version Major", d.VersionMajor]);
		rows.Add(["Folder Count", d.CFolders]);
		rows.Add(["File Count", d.CFiles]);
		rows.Add(["Flags", d.Flags, Constants.FlagsDescription(d.Flags)]);
		rows.Add(["Set ID", d.SetID]);
		rows.Add(["Cabinet Index", d.ICabinet]);

		if (d.HasReserve)
		{
			rows.Add(["cbCFHeader", d.CbCFHeader]);
			rows.Add(["cbCFFolder", d.CbCFFolder]);
			rows.Add(["cbCFData", d.CbCFData]);
			if (d.CbCFHeader > 0)
			{
				rows.Add(["Header Reserve Data", d.AbReserve, $"{d.CbCFHeader} bytes"]);
			}
		}

		if (d.HasPrevCabinet)
		{
			rows.Add(["Previous Cabinet", d.SzCabinetPrev.ToCharArray(), d.SzCabinetPrev]);
			rows.Add(["Previous Disk", d.SzDiskPrev.ToCharArray(), d.SzDiskPrev]);
		}

		if (d.HasNextCabinet)
		{
			rows.Add(["Next Cabinet", d.SzCabinetNext.ToCharArray(), d.SzCabinetNext]);
			rows.Add(["Next Disk", d.SzDiskNext.ToCharArray(), d.SzDiskNext]);
		}

		return rows.ToArray();
	}
}
