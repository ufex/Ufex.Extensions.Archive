using System.Text;

namespace Ufex.Extensions.Archive.TAR.Data;

/// <summary>
/// A single 512-byte TAR header record (POSIX ustar / GNU / V7)
/// </summary>
public class TarHeader : Section
{
	// Raw byte fields from the header (stored for display)
	public byte[] NameBytes { get; init; }          // 100 bytes at 0
	public byte[] ModeBytes { get; init; }          //   8 bytes at 100
	public byte[] UidBytes { get; init; }           //   8 bytes at 108
	public byte[] GidBytes { get; init; }           //   8 bytes at 116
	public byte[] SizeBytes { get; init; }          //  12 bytes at 124
	public byte[] MtimeBytes { get; init; }         //  12 bytes at 136
	public byte[] ChecksumBytes { get; init; }      //   8 bytes at 148
	public byte TypeFlag { get; init; }             //   1 byte  at 156
	public byte[] LinkNameBytes { get; init; }      // 100 bytes at 157
	public byte[] MagicBytes { get; init; }         //   6 bytes at 257
	public byte[] VersionBytes { get; init; }       //   2 bytes at 263
	public byte[] UnameBytes { get; init; }         //  32 bytes at 265
	public byte[] GnameBytes { get; init; }         //  32 bytes at 297
	public byte[] DevMajorBytes { get; init; }      //   8 bytes at 329
	public byte[] DevMinorBytes { get; init; }      //   8 bytes at 337
	public byte[] PrefixBytes { get; init; }        // 155 bytes at 345
	public byte[] PadBytes { get; init; }           //  12 bytes at 500

	// Decoded string fields
	public string Name { get; init; }
	public string LinkName { get; init; }
	public string Magic { get; init; }
	public string Version { get; init; }
	public string Uname { get; init; }
	public string Gname { get; init; }
	public string Prefix { get; init; }

	// Decoded numeric fields (from octal ASCII)
	public UInt32 Mode { get; init; }
	public UInt32 Uid { get; init; }
	public UInt32 Gid { get; init; }
	public UInt64 Size { get; init; }
	public UInt64 Mtime { get; init; }
	public UInt32 Checksum { get; init; }
	public UInt32 DevMajor { get; init; }
	public UInt32 DevMinor { get; init; }

	/// <summary>
	/// Computed checksum to verify against stored checksum
	/// </summary>
	public UInt32 ComputedChecksum { get; init; }

	/// <summary>
	/// Whether this header has a valid checksum
	/// </summary>
	public bool IsChecksumValid => Checksum == ComputedChecksum;

	/// <summary>
	/// Full path name, combining prefix and name
	/// </summary>
	public string FullName
	{
		get
		{
			if (Prefix.Length > 0)
				return Prefix + "/" + Name;
			return Name;
		}
	}

	/// <summary>
	/// The typeflag as a char
	/// </summary>
	public char TypeFlagChar => TypeFlag == 0 ? '\0' : (char)TypeFlag;

	/// <summary>
	/// Whether this is a ustar format header
	/// </summary>
	public bool IsUstar => Magic.StartsWith("ustar");

	/// <summary>
	/// Whether this is a GNU format header
	/// </summary>
	public bool IsGnu => Magic == "ustar  " || Magic.StartsWith("ustar ");

	/// <summary>
	/// File offset where the data for this entry begins (immediately after the header)
	/// </summary>
	public long DataOffset { get; init; }

	/// <summary>
	/// Number of 512-byte blocks the data occupies (including padding)
	/// </summary>
	public long DataBlocks => (long)((Size + 511) / 512);

	/// <summary>
	/// End position of the data (including padding to 512-byte boundary)
	/// </summary>
	public long DataEndPosition => DataOffset + DataBlocks * Signatures.BlockSize;

	public string TypeFlagText => Constants.TypeFlagDescription(TypeFlagChar);
	public string ModeText => Constants.ModeDescription(Mode);
	public string MtimeText => Constants.UnixTimeDescription(Mtime);
	public string FormatText => Constants.FormatDescription(Magic, Version);

	public TarHeader(byte[] headerBlock, long headerPosition)
	{
		StartRead(headerPosition);

		// Extract raw byte fields
		NameBytes = headerBlock[0..100];
		ModeBytes = headerBlock[100..108];
		UidBytes = headerBlock[108..116];
		GidBytes = headerBlock[116..124];
		SizeBytes = headerBlock[124..136];
		MtimeBytes = headerBlock[136..148];
		ChecksumBytes = headerBlock[148..156];
		TypeFlag = headerBlock[156];
		LinkNameBytes = headerBlock[157..257];
		MagicBytes = headerBlock[257..263];
		VersionBytes = headerBlock[263..265];
		UnameBytes = headerBlock[265..297];
		GnameBytes = headerBlock[297..329];
		DevMajorBytes = headerBlock[329..337];
		DevMinorBytes = headerBlock[337..345];
		PrefixBytes = headerBlock[345..500];
		PadBytes = headerBlock[500..512];

		// Decode strings
		Name = DecodeString(NameBytes);
		LinkName = DecodeString(LinkNameBytes);
		Magic = DecodeString(MagicBytes);
		Version = DecodeString(VersionBytes);
		Uname = DecodeString(UnameBytes);
		Gname = DecodeString(GnameBytes);
		Prefix = DecodeString(PrefixBytes);

		// Decode octal numbers
		Mode = (UInt32)ParseOctal(ModeBytes);
		Uid = (UInt32)ParseOctal(UidBytes);
		Gid = (UInt32)ParseOctal(GidBytes);
		Size = ParseSizeField(SizeBytes);
		Mtime = ParseSizeField(MtimeBytes);
		Checksum = (UInt32)ParseOctal(ChecksumBytes);
		DevMajor = (UInt32)ParseOctal(DevMajorBytes);
		DevMinor = (UInt32)ParseOctal(DevMinorBytes);

		// Compute checksum for validation
		ComputedChecksum = ComputeChecksum(headerBlock);

		DataOffset = headerPosition + Signatures.BlockSize;
		EndRead(headerPosition + Signatures.BlockSize);
	}

	/// <summary>
	/// Checks if a 512-byte block is all zeros (end-of-archive marker)
	/// </summary>
	public static bool IsZeroBlock(byte[] block)
	{
		for (int i = 0; i < block.Length; i++)
		{
			if (block[i] != 0) return false;
		}
		return true;
	}

	/// <summary>
	/// Computes the header checksum per the TAR spec:
	/// sum all bytes treating the checksum field (bytes 148-155) as spaces
	/// </summary>
	private static UInt32 ComputeChecksum(byte[] header)
	{
		UInt32 sum = 0;
		for (int i = 0; i < 512; i++)
		{
			if (i >= 148 && i < 156)
				sum += 32; // treat checksum field as spaces
			else
				sum += header[i];
		}
		return sum;
	}

	/// <summary>
	/// Decodes a NUL-terminated ASCII string from a byte array
	/// </summary>
	private static string DecodeString(byte[] data)
	{
		int len = Array.IndexOf(data, (byte)0);
		if (len < 0) len = data.Length;
		return Encoding.ASCII.GetString(data, 0, len);
	}

	/// <summary>
	/// Parses an octal number from a NUL/space-terminated ASCII field
	/// </summary>
	private static UInt64 ParseOctal(byte[] data)
	{
		// Handle GNU binary extension: high bit set means binary encoding
		if (data.Length > 0 && (data[0] & 0x80) != 0)
		{
			return ParseBinaryField(data);
		}

		string str = DecodeString(data).Trim().TrimEnd(' ');
		if (str.Length == 0) return 0;
		try
		{
			return Convert.ToUInt64(str, 8);
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Parses a size field which may be octal or binary-encoded (GNU extension)
	/// </summary>
	private static UInt64 ParseSizeField(byte[] data)
	{
		if (data.Length > 0 && (data[0] & 0x80) != 0)
		{
			return ParseBinaryField(data);
		}
		return ParseOctal(data);
	}

	/// <summary>
	/// Parses a GNU binary-encoded numeric field (high bit set, big-endian)
	/// </summary>
	private static UInt64 ParseBinaryField(byte[] data)
	{
		UInt64 value = 0;
		// Skip the first byte's high bit (marker), use remaining 7 bits
		value = (UInt64)(data[0] & 0x7F);
		for (int i = 1; i < data.Length; i++)
		{
			value = (value << 8) | data[i];
		}
		return value;
	}
}
