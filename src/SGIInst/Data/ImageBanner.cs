namespace Ufex.Extensions.Archive.SGIInst.Data;

/// <summary>
/// Represents the version banner at the start of an image file.
/// Variable-length, NUL-terminated ASCII.
/// </summary>
public class ImageBanner : Section
{
	/// <summary>
	/// The banner text (ASCII, without the NUL terminator)
	/// </summary>
	public string Text { get; init; }

	/// <summary>
	/// Raw bytes including the NUL terminator
	/// </summary>
	public byte[] RawBytes { get; init; }

	/// <summary>
	/// The offset where file regions begin (immediately after the NUL)
	/// </summary>
	public long FirstRegionOffset => EndPosition;

	public ImageBanner(string text, byte[] rawBytes, long position)
	{
		Text = text;
		RawBytes = rawBytes;
		StartRead(position);
		EndRead(position + rawBytes.Length);
	}
}
