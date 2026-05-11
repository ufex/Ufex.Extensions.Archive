namespace Ufex.Extensions.Archive.TAR.Data;

/// <summary>
/// Lookup tables and constant descriptions for TAR format fields
/// </summary>
internal static class Constants
{
	/// <summary>
	/// Describes the typeflag field
	/// </summary>
	public static string TypeFlagDescription(char typeFlag)
	{
		return typeFlag switch
		{
			'\0' or '0' => "Regular file",
			'1' => "Hard link",
			'2' => "Symbolic link",
			'3' => "Character device",
			'4' => "Block device",
			'5' => "Directory",
			'6' => "FIFO",
			'7' => "Contiguous file",
			'g' => "PAX global extended header",
			'x' => "PAX extended header",
			'D' => "GNU directory dump",
			'K' => "GNU long link name",
			'L' => "GNU long file name",
			'M' => "GNU multi-volume continuation",
			'S' => "GNU sparse file",
			'V' => "GNU volume header",
			_ => $"Unknown ({typeFlag})"
		};
	}

	/// <summary>
	/// Describes the TAR format variant based on magic and version
	/// </summary>
	public static string FormatDescription(string magic, string version)
	{
		if (magic.StartsWith(Signatures.UstarMagic))
		{
			if (version == Signatures.UstarVersion)
				return "POSIX ustar";
			return "Pre-POSIX ustar";
		}
		if (magic.StartsWith("ustar "))
			return "GNU tar";
		return "V7 (old-style)";
	}

	/// <summary>
	/// Describes file mode bits as a Unix permission string (e.g., rwxr-xr--)
	/// </summary>
	public static string ModeDescription(UInt32 mode)
	{
		char[] perms = [
			(mode & 0x100) != 0 ? 'r' : '-',
			(mode & 0x080) != 0 ? 'w' : '-',
			(mode & 0x040) != 0 ? ((mode & 0x800) != 0 ? 's' : 'x') : ((mode & 0x800) != 0 ? 'S' : '-'),
			(mode & 0x020) != 0 ? 'r' : '-',
			(mode & 0x010) != 0 ? 'w' : '-',
			(mode & 0x008) != 0 ? ((mode & 0x400) != 0 ? 's' : 'x') : ((mode & 0x400) != 0 ? 'S' : '-'),
			(mode & 0x004) != 0 ? 'r' : '-',
			(mode & 0x002) != 0 ? 'w' : '-',
			(mode & 0x001) != 0 ? ((mode & 0x200) != 0 ? 't' : 'x') : ((mode & 0x200) != 0 ? 'T' : '-'),
		];
		return new string(perms);
	}

	/// <summary>
	/// Converts a Unix timestamp to a human-readable date string
	/// </summary>
	public static string UnixTimeDescription(UInt64 unixTime)
	{
		try
		{
			var dt = DateTimeOffset.FromUnixTimeSeconds((long)unixTime);
			return dt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
		}
		catch
		{
			return $"{unixTime} (invalid)";
		}
	}
}
