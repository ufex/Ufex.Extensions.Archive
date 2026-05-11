using Ufex.API;

namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// CFHEADER - Cabinet file header structure
/// </summary>
public class CfHeader : Section
{
	public byte[] Signature { get; init; }
	public UInt32 Reserved1 { get; init; }
	public UInt32 CbCabinet { get; init; }
	public UInt32 Reserved2 { get; init; }
	public UInt32 CoffFiles { get; init; }
	public UInt32 Reserved3 { get; init; }
	public byte VersionMinor { get; init; }
	public byte VersionMajor { get; init; }
	public UInt16 CFolders { get; init; }
	public UInt16 CFiles { get; init; }
	public UInt16 Flags { get; init; }
	public UInt16 SetID { get; init; }
	public UInt16 ICabinet { get; init; }

	// Optional fields (when cfhdrRESERVE_PRESENT is set)
	public UInt16 CbCFHeader { get; init; }
	public byte CbCFFolder { get; init; }
	public byte CbCFData { get; init; }
	public byte[] AbReserve { get; init; } = [];

	// Optional fields (when cfhdrPREV_CABINET is set)
	public string SzCabinetPrev { get; init; } = "";
	public string SzDiskPrev { get; init; } = "";

	// Optional fields (when cfhdrNEXT_CABINET is set)
	public string SzCabinetNext { get; init; } = "";
	public string SzDiskNext { get; init; } = "";

	public bool HasPrevCabinet => (Flags & Signatures.CfhdrPrevCabinet) != 0;
	public bool HasNextCabinet => (Flags & Signatures.CfhdrNextCabinet) != 0;
	public bool HasReserve => (Flags & Signatures.CfhdrReservePresent) != 0;

	public string VersionText => $"{VersionMajor}.{VersionMinor}";

	public CfHeader(FileReader fr)
	{
		StartRead(fr);
		Signature = fr.ReadBytes(4);
		Reserved1 = fr.ReadUInt32();
		CbCabinet = fr.ReadUInt32();
		Reserved2 = fr.ReadUInt32();
		CoffFiles = fr.ReadUInt32();
		Reserved3 = fr.ReadUInt32();
		VersionMinor = fr.ReadByte();
		VersionMajor = fr.ReadByte();
		CFolders = fr.ReadUInt16();
		CFiles = fr.ReadUInt16();
		Flags = fr.ReadUInt16();
		SetID = fr.ReadUInt16();
		ICabinet = fr.ReadUInt16();

		// Optional reserve fields
		if (HasReserve)
		{
			CbCFHeader = fr.ReadUInt16();
			CbCFFolder = fr.ReadByte();
			CbCFData = fr.ReadByte();
			if (CbCFHeader > 0)
			{
				AbReserve = fr.ReadBytes(CbCFHeader);
			}
		}

		// Optional prev cabinet fields
		if (HasPrevCabinet)
		{
			SzCabinetPrev = ReadNullTerminatedString(fr);
			SzDiskPrev = ReadNullTerminatedString(fr);
		}

		// Optional next cabinet fields
		if (HasNextCabinet)
		{
			SzCabinetNext = ReadNullTerminatedString(fr);
			SzDiskNext = ReadNullTerminatedString(fr);
		}
		EndRead(fr);
	}

	/// <summary>
	/// Reads a NUL-terminated string from the FileReader
	/// </summary>
	private static string ReadNullTerminatedString(FileReader fr)
	{
		var bytes = new List<byte>();
		byte b;
		while ((b = fr.ReadByte()) != 0)
		{
			bytes.Add(b);
		}
		return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
	}
}
