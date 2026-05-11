using Ufex.API;

namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// CFFOLDER - Cabinet folder entry
/// </summary>
public class CfFolder : Section
{
	public UInt32 CoffCabStart { get; init; }
	public UInt16 CCFData { get; init; }
	public UInt16 TypeCompress { get; init; }
	public byte[] AbReserve { get; init; } = [];

	/// <summary>
	/// Compression type description
	/// </summary>
	public string TypeCompressText => Constants.CompressionTypeDescription(TypeCompress);

	public CfFolder(FileReader fr, byte cbCFFolder)
	{
		StartRead(fr);
		CoffCabStart = fr.ReadUInt32();
		CCFData = fr.ReadUInt16();
		TypeCompress = fr.ReadUInt16();
		if (cbCFFolder > 0)
		{
			AbReserve = fr.ReadBytes(cbCFFolder);
		}
		EndRead(fr);
	}
}
