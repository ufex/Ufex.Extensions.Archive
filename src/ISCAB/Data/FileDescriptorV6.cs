using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// File descriptor for ISCAB v6+ (87 bytes / 0x57)
/// </summary>
public class FileDescriptorV6 : Section
{
	public UInt16 Flags { get; init; }
	public UInt64 ExpandedSize { get; init; }
	public UInt64 CompressedSize { get; init; }
	public UInt64 DataOffset { get; init; }
	public byte[] Md5 { get; init; }
	public byte[] Unknown1 { get; init; }
	public UInt32 NameOffset { get; init; }
	public UInt16 DirectoryIndex { get; init; }
	public byte[] Unknown2 { get; init; }
	public UInt32 LinkPrevious { get; init; }
	public UInt32 LinkNext { get; init; }
	public byte LinkFlags { get; init; }
	public UInt16 Volume { get; init; }

	/// <summary>
	/// Resolved file name (set after parsing)
	/// </summary>
	public string FileName { get; set; } = "";

	/// <summary>
	/// Resolved directory name (set after parsing)
	/// </summary>
	public string DirectoryName { get; set; } = "";

	public string FlagsText => Constants.FileFlagsDescription(Flags);
	public string LinkFlagsText => Constants.LinkFlagsDescription(LinkFlags);

	public FileDescriptorV6(FileReader fr)
	{
		StartRead(fr);
		Flags = fr.ReadUInt16();            // 0x00
		ExpandedSize = fr.ReadUInt64();     // 0x02
		CompressedSize = fr.ReadUInt64();   // 0x0A
		DataOffset = fr.ReadUInt64();       // 0x12
		Md5 = fr.ReadBytes(16);             // 0x1A
		Unknown1 = fr.ReadBytes(16);        // 0x2A
		NameOffset = fr.ReadUInt32();       // 0x3A
		DirectoryIndex = fr.ReadUInt16();   // 0x3E
		Unknown2 = fr.ReadBytes(12);        // 0x40
		LinkPrevious = fr.ReadUInt32();     // 0x4C
		LinkNext = fr.ReadUInt32();         // 0x50
		LinkFlags = fr.ReadByte();          // 0x54
		Volume = fr.ReadUInt16();           // 0x55
		EndRead(fr);                         // 0x57
	}
}
