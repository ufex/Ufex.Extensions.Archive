namespace Ufex.Extensions.Archive.TAR.Data;

/// <summary>
/// A complete TAR entry: header + data region reference
/// </summary>
public class TarEntry : Section
{
	/// <summary>
	/// The parsed 512-byte header
	/// </summary>
	public TarHeader Header { get; init; }

	/// <summary>
	/// For GNU long name (typeflag 'L'), the resolved long name
	/// </summary>
	public string? LongName { get; set; }

	/// <summary>
	/// For GNU long link (typeflag 'K'), the resolved long link name
	/// </summary>
	public string? LongLinkName { get; set; }

	/// <summary>
	/// Display name: uses long name if available, otherwise header name
	/// </summary>
	public string DisplayName => LongName ?? Header.FullName;

	/// <summary>
	/// Display link name: uses long link name if available, otherwise header link name
	/// </summary>
	public string DisplayLinkName => LongLinkName ?? Header.LinkName;

	public TarEntry(TarHeader header)
	{
		Header = header;
		StartRead(header.StartPosition);
		EndRead(header.DataEndPosition);
	}
}
