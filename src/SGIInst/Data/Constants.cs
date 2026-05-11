namespace Ufex.Extensions.Archive.SGIInst.Data;

public static class Constants
{
	/// <summary>
	/// IDB entry type descriptions
	/// </summary>
	public static string EntryTypeDescription(char type) => type switch
	{
		'f' => "Regular File",
		'd' => "Directory",
		'l' => "Symbolic Link",
		'b' => "Block Device",
		'c' => "Character Device",
		'p' => "FIFO",
		_ => $"Unknown ({type})"
	};

	/// <summary>
	/// Decode octal mode to a Unix permission string (e.g. "rwxr-xr-x")
	/// </summary>
	public static string ModeDescription(UInt32 mode)
	{
		char[] perm = new char[9];
		perm[0] = (mode & 0x100) != 0 ? 'r' : '-';
		perm[1] = (mode & 0x080) != 0 ? 'w' : '-';
		perm[2] = (mode & 0x800) != 0 ? ((mode & 0x040) != 0 ? 's' : 'S') :
		           (mode & 0x040) != 0 ? 'x' : '-';
		perm[3] = (mode & 0x020) != 0 ? 'r' : '-';
		perm[4] = (mode & 0x010) != 0 ? 'w' : '-';
		perm[5] = (mode & 0x400) != 0 ? ((mode & 0x008) != 0 ? 's' : 'S') :
		           (mode & 0x008) != 0 ? 'x' : '-';
		perm[6] = (mode & 0x004) != 0 ? 'r' : '-';
		perm[7] = (mode & 0x002) != 0 ? 'w' : '-';
		perm[8] = (mode & 0x200) != 0 ? ((mode & 0x001) != 0 ? 't' : 'T') :
		           (mode & 0x001) != 0 ? 'x' : '-';
		return new string(perm);
	}

	/// <summary>
	/// BSD compress magic bytes
	/// </summary>
	public const byte CompressMagic1 = 0x1F;
	public const byte CompressMagic2 = 0x9D;
}
