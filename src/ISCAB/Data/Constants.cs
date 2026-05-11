namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// Lookup tables and descriptions for ISCAB format fields
/// </summary>
internal static class Constants
{
	/// <summary>
	/// Size of the v5 file descriptor in bytes
	/// </summary>
	public const Int32 FileDescriptorV5Size = 0x3A;

	/// <summary>
	/// Size of the v6+ file descriptor in bytes
	/// </summary>
	public const Int32 FileDescriptorV6Size = 0x57;

	/// <summary>
	/// Size of the v5 volume header in bytes
	/// </summary>
	public const Int32 VolumeHeaderV5Size = 0x28;

	/// <summary>
	/// Size of the v6+ volume header in bytes
	/// </summary>
	public const Int32 VolumeHeaderV6Size = 0x40;

	/// <summary>
	/// Describes file flags bitfield
	/// </summary>
	public static string FileFlagsDescription(UInt16 flags)
	{
		var parts = new List<string>();
		if ((flags & Signatures.FileSplit) != 0) parts.Add("SPLIT");
		if ((flags & Signatures.FileObfuscated) != 0) parts.Add("OBFUSCATED");
		if ((flags & Signatures.FileCompressed) != 0) parts.Add("COMPRESSED");
		if ((flags & Signatures.FileInvalid) != 0) parts.Add("INVALID");
		return parts.Count > 0 ? string.Join(" | ", parts) : "None";
	}

	/// <summary>
	/// Describes link flags (v6+)
	/// </summary>
	public static string LinkFlagsDescription(byte linkFlags)
	{
		return linkFlags switch
		{
			Signatures.LinkNone => "None",
			Signatures.LinkPrev => "LINK_PREV",
			Signatures.LinkNext => "LINK_NEXT",
			Signatures.LinkBoth => "LINK_BOTH",
			_ => $"Unknown (0x{linkFlags:X2})"
		};
	}

	/// <summary>
	/// Computes the major version from the raw version field per §4
	/// </summary>
	public static Int32 ComputeMajorVersion(UInt32 rawVersion)
	{
		byte highByte = (byte)((rawVersion >> 24) & 0xFF);
		return highByte switch
		{
			1 => (Int32)((rawVersion >> 12) & 0x0F),
			2 or 4 => ComputeMajorFromLowWord(rawVersion),
			_ => 0
		};
	}

	private static Int32 ComputeMajorFromLowWord(UInt32 rawVersion)
	{
		UInt32 candidate = rawVersion & 0xFFFF;
		if (candidate == 0) return 0;
		return (Int32)(candidate / 100);
	}
}
