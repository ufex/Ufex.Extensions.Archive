using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// File descriptor for ISCAB v5 (58 bytes / 0x3A)
/// </summary>
public class FileDescriptorV5 : Section
{
	public UInt32 NameOffset { get; init; }
	public UInt16 DirectoryIndex { get; init; }
	public byte[] Unknown1 { get; init; }
	public UInt16 Flags { get; init; }
	public UInt32 ExpandedSize { get; init; }
	public UInt32 CompressedSize { get; init; }
	public byte[] Unknown2 { get; init; }
	public UInt32 DataOffset { get; init; }
	public byte[] Md5 { get; init; }

	/// <summary>
	/// Resolved file name (set after parsing)
	/// </summary>
	public string FileName { get; set; } = "";

	/// <summary>
	/// Resolved directory name (set after parsing)
	/// </summary>
	public string DirectoryName { get; set; } = "";

	public string FlagsText => Constants.FileFlagsDescription(Flags);

	public FileDescriptorV5(FileReader fr)
	{
		StartRead(fr);
		NameOffset = fr.ReadUInt32();       // 0x00
		DirectoryIndex = fr.ReadUInt16();   // 0x04
		Unknown1 = fr.ReadBytes(2);         // 0x06
		Flags = fr.ReadUInt16();            // 0x08
		ExpandedSize = fr.ReadUInt32();     // 0x0A
		CompressedSize = fr.ReadUInt32();   // 0x0E
		Unknown2 = fr.ReadBytes(0x14);      // 0x12
		DataOffset = fr.ReadUInt32();       // 0x26
		Md5 = fr.ReadBytes(16);             // 0x2A
		EndRead(fr);                         // 0x3A
	}
}
