using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// Component descriptor - a named grouping of file groups
/// </summary>
public class ComponentDescriptor : Section
{
	public UInt32 NameOffset { get; init; }
	public byte[] Unknown { get; init; }
	public UInt16 FileGroupCount { get; init; }
	public UInt32 FileGroupTableOffset { get; init; }

	/// <summary>
	/// Resolved name (set after parsing)
	/// </summary>
	public string Name { get; set; } = "";

	public ComponentDescriptor(FileReader fr, Int32 majorVersion)
	{
		StartRead(fr);
		NameOffset = fr.ReadUInt32();   // 0x00
		// Unknown region: 0x6C bytes for major versions 0/5, 0x6B for major version >= 6
		Int32 unknownSize = majorVersion >= 6 ? 0x6B : 0x6C;
		Unknown = fr.ReadBytes(unknownSize);
		FileGroupCount = fr.ReadUInt16();
		FileGroupTableOffset = fr.ReadUInt32();
		EndRead(fr);
	}
}
