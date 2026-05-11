namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// CAB file signatures and constants
/// </summary>
internal static class Signatures
{
	/// <summary>
	/// Cabinet file signature: 'M','S','C','F' (0x4D, 0x53, 0x43, 0x46)
	/// </summary>
	public static readonly byte[] FileSignature = [0x4D, 0x53, 0x43, 0x46];

	// CFHEADER flags
	public const UInt16 CfhdrPrevCabinet = 0x0001;
	public const UInt16 CfhdrNextCabinet = 0x0002;
	public const UInt16 CfhdrReservePresent = 0x0004;

	// CFFILE iFolder special values
	public const UInt16 IfoldContinuedFromPrev = 0xFFFD;
	public const UInt16 IfoldContinuedToNext = 0xFFFE;
	public const UInt16 IfoldContinuedPrevAndNext = 0xFFFF;

	// CFFILE attribs
	public const UInt16 AttrReadOnly = 0x01;
	public const UInt16 AttrHidden = 0x02;
	public const UInt16 AttrSystem = 0x04;
	public const UInt16 AttrArchive = 0x20;
	public const UInt16 AttrExec = 0x40;
	public const UInt16 AttrNameIsUtf = 0x80;
}
