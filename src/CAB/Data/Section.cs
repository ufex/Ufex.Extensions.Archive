using System.Text;

namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// Base class for all CAB file sections
/// </summary>
public abstract class Section
{
	public long StartPosition { get; protected set; }
	public long EndPosition { get; protected set; }

	protected void StartRead(BinaryReader br)
	{
		StartPosition = br.BaseStream.Position;
	}

	protected void EndRead(BinaryReader br)
	{
		EndPosition = br.BaseStream.Position;
	}

	/// <summary>
	/// Decodes a DOS date format into a string representation YYYY-MM-DD
	/// </summary>
	protected static string DecodeDate(UInt16 date)
	{
		Int32 year = ((date >> 9) & 0x7F) + 1980;
		Int32 month = (date >> 5) & 0x0F;
		Int32 day = date & 0x1F;
		return $"{year:D4}-{month:D2}-{day:D2}";
	}

	/// <summary>
	/// Decodes a DOS time format into a string representation HH:MM:SS
	/// </summary>
	protected static string DecodeTime(UInt16 time)
	{
		Int32 hours = (time >> 11) & 0x1F;
		Int32 minutes = (time >> 5) & 0x3F;
		Int32 seconds = (time & 0x1F) * 2;
		return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
	}

	/// <summary>
	/// Reads a NUL-terminated ASCII/UTF string from the stream
	/// </summary>
	protected static string ReadNullTerminatedString(BinaryReader br)
	{
		var bytes = new List<byte>();
		byte b;
		while ((b = br.ReadByte()) != 0)
		{
			bytes.Add(b);
		}
		return Encoding.ASCII.GetString(bytes.ToArray());
	}
}
