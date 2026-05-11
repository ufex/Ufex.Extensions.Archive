using System.Text;
using Ufex.API;

namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// CFFILE - Cabinet file entry
/// </summary>
public class CfFile : Section
{
	public UInt32 CbFile { get; init; }
	public UInt32 UoffFolderStart { get; init; }
	public UInt16 IFolder { get; init; }
	public UInt16 Date { get; init; }
	public UInt16 Time { get; init; }
	public UInt16 Attribs { get; init; }
	public byte[] SzNameBytes { get; init; } = [];

	/// <summary>
	/// Decoded file name
	/// </summary>
	public string SzName
	{
		get
		{
			if ((Attribs & Signatures.AttrNameIsUtf) != 0)
				return Encoding.UTF8.GetString(SzNameBytes);
			return Encoding.ASCII.GetString(SzNameBytes);
		}
	}

	public string DateText => DecodeDate(Date);
	public string TimeText => DecodeTime(Time);
	public string AttribsText => Constants.AttribsDescription(Attribs);

	public CfFile(FileReader fr)
	{
		StartRead(fr);
		CbFile = fr.ReadUInt32();
		UoffFolderStart = fr.ReadUInt32();
		IFolder = fr.ReadUInt16();
		Date = fr.ReadUInt16();
		Time = fr.ReadUInt16();
		Attribs = fr.ReadUInt16();

		// Read null-terminated file name
		var nameBytes = new List<byte>();
		byte b;
		while ((b = fr.ReadByte()) != 0)
		{
			nameBytes.Add(b);
		}
		SzNameBytes = nameBytes.ToArray();
		EndRead(fr);
	}
}
