namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// ISCAB file signatures, constants, and magic numbers
/// </summary>
internal static class Signatures
{
	/// <summary>
	/// ISCAB Common Header signature: "ISc(" = 0x28635349 (little-endian: 49 53 63 28)
	/// </summary>
	public const UInt32 CabSignature = 0x28635349;

	/// <summary>
	/// Microsoft Cabinet signature (NOT this format): "MSCF" = 0x4643534D
	/// </summary>
	public const UInt32 MscfSignature = 0x4643534D;

	/// <summary>
	/// Number of entries in file_group_offsets and component_offsets arrays
	/// </summary>
	public const Int32 OffsetCount = 71;

	// File descriptor flags
	public const UInt16 FileSplit = 0x0001;
	public const UInt16 FileObfuscated = 0x0002;
	public const UInt16 FileCompressed = 0x0004;
	public const UInt16 FileInvalid = 0x0008;

	// Link flags (v6+ only)
	public const byte LinkNone = 0x00;
	public const byte LinkPrev = 0x01;
	public const byte LinkNext = 0x02;
	public const byte LinkBoth = 0x03;
}
