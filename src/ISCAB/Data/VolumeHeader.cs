using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// Volume Header - appears after Common Header in every .cab file.
/// Layout differs between v5 (40 bytes) and v6+ (64 bytes).
/// </summary>
public class VolumeHeader : Section
{
	public UInt32 DataOffset { get; init; }
	public UInt32 DataOffsetHigh { get; init; }
	public UInt32 FirstFileIndex { get; init; }
	public UInt32 LastFileIndex { get; init; }
	public UInt32 FirstFileOffset { get; init; }
	public UInt32 FirstFileOffsetHigh { get; init; }
	public UInt32 FirstFileSizeExpanded { get; init; }
	public UInt32 FirstFileSizeExpandedHigh { get; init; }
	public UInt32 FirstFileSizeCompressed { get; init; }
	public UInt32 FirstFileSizeCompressedHigh { get; init; }
	public UInt32 LastFileOffset { get; init; }
	public UInt32 LastFileOffsetHigh { get; init; }
	public UInt32 LastFileSizeExpanded { get; init; }
	public UInt32 LastFileSizeExpandedHigh { get; init; }
	public UInt32 LastFileSizeCompressed { get; init; }
	public UInt32 LastFileSizeCompressedHigh { get; init; }

	public bool IsV6 { get; init; }

	public UInt64 DataOffset64 => DataOffset | ((UInt64)DataOffsetHigh << 32);
	public UInt64 FirstFileOffset64 => FirstFileOffset | ((UInt64)FirstFileOffsetHigh << 32);
	public UInt64 LastFileOffset64 => LastFileOffset | ((UInt64)LastFileOffsetHigh << 32);

	public VolumeHeader(FileReader fr, bool isV6)
	{
		IsV6 = isV6;
		StartRead(fr);

		if (isV6)
		{
			// v6+ format: 64 bytes with high fields
			DataOffset = fr.ReadUInt32();
			DataOffsetHigh = fr.ReadUInt32();
			FirstFileIndex = fr.ReadUInt32();
			LastFileIndex = fr.ReadUInt32();
			FirstFileOffset = fr.ReadUInt32();
			FirstFileOffsetHigh = fr.ReadUInt32();
			FirstFileSizeExpanded = fr.ReadUInt32();
			FirstFileSizeExpandedHigh = fr.ReadUInt32();
			FirstFileSizeCompressed = fr.ReadUInt32();
			FirstFileSizeCompressedHigh = fr.ReadUInt32();
			LastFileOffset = fr.ReadUInt32();
			LastFileOffsetHigh = fr.ReadUInt32();
			LastFileSizeExpanded = fr.ReadUInt32();
			LastFileSizeExpandedHigh = fr.ReadUInt32();
			LastFileSizeCompressed = fr.ReadUInt32();
			LastFileSizeCompressedHigh = fr.ReadUInt32();
		}
		else
		{
			// v5 format: 40 bytes
			DataOffset = fr.ReadUInt32();
			fr.ReadBytes(4); // unknown
			FirstFileIndex = fr.ReadUInt32();
			LastFileIndex = fr.ReadUInt32();
			FirstFileOffset = fr.ReadUInt32();
			FirstFileSizeExpanded = fr.ReadUInt32();
			FirstFileSizeCompressed = fr.ReadUInt32();
			LastFileOffset = fr.ReadUInt32();
			LastFileSizeExpanded = fr.ReadUInt32();
			LastFileSizeCompressed = fr.ReadUInt32();
		}
		EndRead(fr);
	}
}
