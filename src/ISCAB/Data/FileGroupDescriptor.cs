using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// File group descriptor - a named contiguous range of file indices
/// </summary>
public class FileGroupDescriptor : Section
{
	public UInt32 NameOffset { get; init; }
	public byte[] Unknown { get; init; }
	public Int32 FirstFile { get; init; }
	public Int32 LastFile { get; init; }

	/// <summary>
	/// Resolved name (set after parsing)
	/// </summary>
	public string Name { get; set; } = "";

	/// <summary>
	/// Number of files in this group (0 if empty)
	/// </summary>
	public Int32 FileCount => FirstFile <= LastFile && FirstFile >= 0 ? LastFile - FirstFile + 1 : 0;

	public FileGroupDescriptor(FileReader fr, Int32 majorVersion)
	{
		StartRead(fr);
		NameOffset = fr.ReadUInt32();   // 0x00
		// Unknown region: 0x48 bytes for v<=5, 0x12 bytes for v>=6
		Int32 unknownSize = majorVersion >= 6 ? 0x12 : 0x48;
		Unknown = fr.ReadBytes(unknownSize);
		FirstFile = fr.ReadInt32();
		LastFile = fr.ReadInt32();
		EndRead(fr);
	}
}
