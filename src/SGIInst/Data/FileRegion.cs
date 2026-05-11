namespace Ufex.Extensions.Archive.SGIInst.Data;

/// <summary>
/// Represents a file region within an image file.
/// Each region contains a 2-byte pathname length, the pathname, and the payload.
/// </summary>
public class FileRegion : Section
{
	/// <summary>
	/// The 2-byte big-endian pathname length
	/// </summary>
	public UInt16 PathnameLength { get; init; }

	/// <summary>
	/// The pathname as read from the image file
	/// </summary>
	public string Pathname { get; init; } = "";

	/// <summary>
	/// Whether the embedded pathname matches the IDB record's filename
	/// </summary>
	public bool PathnameMatches { get; init; }

	/// <summary>
	/// Offset where the payload bytes begin
	/// </summary>
	public long PayloadOffset { get; init; }

	/// <summary>
	/// Length of the payload in bytes (compressed size if compressed, else uncompressed size)
	/// </summary>
	public Int64 PayloadLength { get; init; }

	/// <summary>
	/// The name of the image file this region belongs to (e.g. "eoe.sw")
	/// </summary>
	public string ImageFileName { get; init; } = "";

	/// <summary>
	/// The corresponding IDB record
	/// </summary>
	public IdbRecord Record { get; init; }

	public FileRegion(IdbRecord record, UInt16 pathnameLength, string pathname,
		bool pathnameMatches, long regionOffset, long payloadOffset, long payloadLength, string imageFileName)
	{
		Record = record;
		PathnameLength = pathnameLength;
		Pathname = pathname;
		PathnameMatches = pathnameMatches;
		PayloadOffset = payloadOffset;
		PayloadLength = payloadLength;
		ImageFileName = imageFileName;
		StartRead(regionOffset);
		EndRead(payloadOffset + payloadLength);
	}
}
