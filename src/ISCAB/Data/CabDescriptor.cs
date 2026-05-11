using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// The CAB Descriptor - central metadata block of an ISCAB archive.
/// Located at file offset common.cab_descriptor_offset in the header file.
/// </summary>
public class CabDescriptor : Section
{
	/// <summary>
	/// Fixed size of the CAB descriptor: 62 bytes of fields + 71*4*2 offset arrays = 630 bytes
	/// </summary>
	public const int FixedSize = 0x276;

	public byte[] Unknown1 { get; init; }
	public UInt32 FileTableOffset { get; init; }
	public byte[] Unknown2 { get; init; }
	public UInt32 FileTableSize { get; init; }
	public UInt32 FileTableSize2 { get; init; }
	public UInt32 DirectoryCount { get; init; }
	public byte[] Unknown3 { get; init; }
	public UInt32 FileCount { get; init; }
	public UInt32 FileTableOffset2 { get; init; }
	public byte[] Unknown4 { get; init; }
	public UInt32[] FileGroupOffsets { get; init; }
	public UInt32[] ComponentOffsets { get; init; }

	public CabDescriptor(FileReader fr)
	{
		StartRead(fr);
		Unknown1 = fr.ReadBytes(12);            // 0x00 - 0x0B
		FileTableOffset = fr.ReadUInt32();       // 0x0C
		Unknown2 = fr.ReadBytes(4);             // 0x10
		FileTableSize = fr.ReadUInt32();         // 0x14
		FileTableSize2 = fr.ReadUInt32();        // 0x18
		DirectoryCount = fr.ReadUInt32();        // 0x1C
		Unknown3 = fr.ReadBytes(8);             // 0x20
		FileCount = fr.ReadUInt32();             // 0x28
		FileTableOffset2 = fr.ReadUInt32();      // 0x2C
		Unknown4 = fr.ReadBytes(14);            // 0x30

		// file_group_offsets[71] at 0x3E
		FileGroupOffsets = new UInt32[Signatures.OffsetCount];
		for (int i = 0; i < Signatures.OffsetCount; i++)
		{
			FileGroupOffsets[i] = fr.ReadUInt32();
		}

		// component_offsets[71] at 0x15A
		ComponentOffsets = new UInt32[Signatures.OffsetCount];
		for (int i = 0; i < Signatures.OffsetCount; i++)
		{
			ComponentOffsets[i] = fr.ReadUInt32();
		}
		EndRead(fr);
	}
}
