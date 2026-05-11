using Ufex.API;

namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// CFDATA - Cabinet data block
/// </summary>
public class CfData : Section
{
	public UInt32 Checksum { get; init; }
	public UInt16 CbData { get; init; }
	public UInt16 CbUncomp { get; init; }
	public byte[] AbReserve { get; init; } = [];

	/// <summary>
	/// File offset where the compressed data payload begins
	/// </summary>
	public long DataOffset { get; init; }

	public CfData(FileReader fr, byte cbCFData)
	{
		StartRead(fr);
		Checksum = fr.ReadUInt32();
		CbData = fr.ReadUInt16();
		CbUncomp = fr.ReadUInt16();
		if (cbCFData > 0)
		{
			AbReserve = fr.ReadBytes(cbCFData);
		}
		DataOffset = fr.BaseStream.Position;
		// Skip past the compressed data
		fr.BaseStream.Seek(CbData, SeekOrigin.Current);
		EndRead(fr);
	}
}
