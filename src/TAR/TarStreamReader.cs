using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Archive.TAR.Data;

namespace Ufex.Extensions.Archive.TAR;

public class TarStreamReader
{
	private Stream _fileStream;

	public List<TarEntry> Entries { get; init; }

	/// <summary>
	/// Detected format variant (ustar, GNU, V7)
	/// </summary>
	public string Format { get; private set; } = "Unknown";

	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	public TarStreamReader(Stream fileStream, Logger log, ValidationReport validationReport)
	{
		_fileStream = fileStream;
		Entries = new List<TarEntry>();
		Log = log;
		ValidationReport = validationReport;
	}

	public bool Read()
	{
		byte[] block = new byte[Signatures.BlockSize];
		int zeroBlockCount = 0;
		string? pendingLongName = null;
		string? pendingLongLinkName = null;
		bool formatDetected = false;

		while (_fileStream.Position + Signatures.BlockSize <= _fileStream.Length)
		{
			long blockPosition = _fileStream.Position;
			int bytesRead = _fileStream.Read(block, 0, Signatures.BlockSize);
			if (bytesRead < Signatures.BlockSize)
			{
				ValidationReport.Warning($"Incomplete block at offset 0x{blockPosition:X}");
				break;
			}

			// Check for end-of-archive (two consecutive zero blocks)
			if (TarHeader.IsZeroBlock(block))
			{
				zeroBlockCount++;
				if (zeroBlockCount >= 2)
				{
					Log.LogInformation("End of archive marker found");
					break;
				}
				continue;
			}
			zeroBlockCount = 0;

			var header = new TarHeader(block, blockPosition);

			// Detect format from the first valid header
			if (!formatDetected)
			{
				Format = header.FormatText;
				formatDetected = true;
			}

			// Validate checksum
			if (!header.IsChecksumValid)
			{
				ValidationReport.Warning($"Checksum mismatch at offset 0x{blockPosition:X}: stored={header.Checksum}, computed={header.ComputedChecksum}");
			}

			Log.LogInformation($"Entry at 0x{blockPosition:X}: {header.FullName}, type={header.TypeFlagChar}, size={header.Size}");

			// Handle GNU long name extension
			if (header.TypeFlagChar == 'L')
			{
				pendingLongName = ReadLongString(header.Size);
				Log.LogInformation($"GNU long name: {pendingLongName}");
				continue;
			}

			// Handle GNU long link name extension
			if (header.TypeFlagChar == 'K')
			{
				pendingLongLinkName = ReadLongString(header.Size);
				Log.LogInformation($"GNU long link name: {pendingLongLinkName}");
				continue;
			}

			// Handle PAX extended headers (skip the data, parse the next real entry)
			if (header.TypeFlagChar == 'x' || header.TypeFlagChar == 'g')
			{
				// Skip PAX header data for now (could be parsed for extended attributes)
				SkipDataBlocks(header.Size);
				Log.LogInformation($"Skipped PAX header ({header.Size} bytes)");
				continue;
			}

			var entry = new TarEntry(header);

			// Apply pending GNU long name/link
			if (pendingLongName != null)
			{
				entry.LongName = pendingLongName;
				pendingLongName = null;
			}
			if (pendingLongLinkName != null)
			{
				entry.LongLinkName = pendingLongLinkName;
				pendingLongLinkName = null;
			}

			Entries.Add(entry);

			// Skip past the file data (padded to 512-byte boundary)
			if (header.Size > 0)
			{
				SkipDataBlocks(header.Size);
			}
		}

		ValidationReport.Info($"Parsed {Entries.Count} entries ({Format} format)");
		return true;
	}

	/// <summary>
	/// Reads a GNU long name string from the data blocks
	/// </summary>
	private string ReadLongString(UInt64 size)
	{
		long blocks = (long)((size + 511) / 512);
		byte[] data = new byte[blocks * Signatures.BlockSize];
		_fileStream.ReadExactly(data, 0, data.Length);

		// The string is NUL-terminated within the data
		int len = Array.IndexOf(data, (byte)0);
		if (len < 0) len = (int)size;
		return Encoding.ASCII.GetString(data, 0, len);
	}

	/// <summary>
	/// Skips past file data blocks (padded to 512-byte boundary)
	/// </summary>
	private void SkipDataBlocks(UInt64 size)
	{
		long blocks = (long)((size + 511) / 512);
		long skipBytes = blocks * Signatures.BlockSize;
		_fileStream.Seek(skipBytes, SeekOrigin.Current);
	}
}
