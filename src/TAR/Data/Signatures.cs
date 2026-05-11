namespace Ufex.Extensions.Archive.TAR.Data;

/// <summary>
/// TAR format constants and magic values
/// </summary>
internal static class Signatures
{
	/// <summary>
	/// POSIX ustar magic: "ustar\0"
	/// </summary>
	public const string UstarMagic = "ustar";

	/// <summary>
	/// Old GNU magic: "ustar  " (with trailing spaces)
	/// </summary>
	public const string GnuMagic = "ustar  ";

	/// <summary>
	/// TAR block size in bytes
	/// </summary>
	public const Int32 BlockSize = 512;

	/// <summary>
	/// POSIX ustar version: "00"
	/// </summary>
	public const string UstarVersion = "00";
}
