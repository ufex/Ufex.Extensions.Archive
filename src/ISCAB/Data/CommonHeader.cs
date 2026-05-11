using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// The Common Header (20 bytes) at the start of every .hdr and .cab file
/// </summary>
public class CommonHeader : Section
{
	public UInt32 Signature { get; init; }
	public UInt32 Version { get; init; }
	public UInt32 VolumeInfo { get; init; }
	public UInt32 CabDescriptorOffset { get; init; }
	public UInt32 CabDescriptorSize { get; init; }

	/// <summary>
	/// Computed major version from the raw version field
	/// </summary>
	public Int32 MajorVersion => Constants.ComputeMajorVersion(Version);

	/// <summary>
	/// Whether this file uses v6+ layouts
	/// </summary>
	public bool IsV6OrLater => MajorVersion >= 6;

	/// <summary>
	/// Whether this file uses UTF-16 strings
	/// </summary>
	public bool IsUnicode => MajorVersion >= 17;

	public CommonHeader(FileReader fr)
	{
		StartRead(fr);
		Signature = fr.ReadUInt32();
		Version = fr.ReadUInt32();
		VolumeInfo = fr.ReadUInt32();
		CabDescriptorOffset = fr.ReadUInt32();
		CabDescriptorSize = fr.ReadUInt32();
		EndRead(fr);
	}
}
