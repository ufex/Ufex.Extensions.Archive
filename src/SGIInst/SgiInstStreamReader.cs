using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Archive.SGIInst.Data;

namespace Ufex.Extensions.Archive.SGIInst;

/// <summary>
/// Reads and parses an SGI inst distribution: the .idb manifest and associated image files.
/// </summary>
public class SgiInstStreamReader
{
	private readonly Stream _idbStream;
	private readonly string? _idbFilePath;

	public List<IdbRecord> Records { get; init; }
	public Dictionary<string, ImageBanner> ImageBanners { get; init; }
	public List<FileRegion> FileRegions { get; init; }

	/// <summary>
	/// Distinct image names found in the IDB (e.g. "sw", "man")
	/// </summary>
	public List<string> ImageNames { get; private set; } = new();

	/// <summary>
	/// The base name of the distribution (e.g. "eoe" from "eoe.idb")
	/// </summary>
	public string BaseName { get; private set; } = "";

	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	public SgiInstStreamReader(Stream idbStream, string? idbFilePath, Logger log, ValidationReport validationReport)
	{
		_idbStream = idbStream;
		_idbFilePath = idbFilePath;
		Records = new List<IdbRecord>();
		ImageBanners = new Dictionary<string, ImageBanner>();
		FileRegions = new List<FileRegion>();
		ImageNames = new List<string>();
		Log = log;
		ValidationReport = validationReport;
	}

	public bool Read()
	{
		// Determine base name from the .idb file path
		if (!string.IsNullOrEmpty(_idbFilePath))
		{
			BaseName = Path.GetFileNameWithoutExtension(_idbFilePath);
		}

		// Parse the IDB text manifest
		ParseIdb();

		// Discover distinct image names
		var imageSet = new HashSet<string>();
		foreach (var record in Records)
		{
			if (record.Type == 'f' && !string.IsNullOrEmpty(record.ImageName))
				imageSet.Add(record.ImageName);
		}
		ImageNames = imageSet.OrderBy(n => n).ToList();

		// Try to read file regions from each image file
		if (!string.IsNullOrEmpty(_idbFilePath))
		{
			string? directory = Path.GetDirectoryName(_idbFilePath);
			if (directory != null)
			{
				foreach (var imageName in ImageNames)
				{
					ReadImageFile(directory, imageName);
				}
			}
		}

		int files = Records.Count(r => r.Type == 'f');
		int dirs = Records.Count(r => r.Type == 'd');
		int links = Records.Count(r => r.Type == 'l');
		ValidationReport.Info($"Parsed {Records.Count} IDB records ({files} files, {dirs} directories, {links} links)");
		ValidationReport.Info($"Found {ImageNames.Count} image file(s), validated {FileRegions.Count} file region(s)");

		return true;
	}

	private void ParseIdb()
	{
		using var reader = new StreamReader(_idbStream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
		int lineNumber = 0;

		while (reader.ReadLine() is { } line)
		{
			lineNumber++;

			// Skip empty lines and NUL-prefixed lines (IRIX 3 artifact)
			if (line.Length == 0 || line[0] == '\0')
				continue;

			var record = ParseRecord(line, lineNumber);
			if (record != null)
			{
				Records.Add(record);
			}
			else
			{
				ValidationReport.Warning($"Could not parse IDB line {lineNumber}");
			}
		}

		Log.LogInformation($"Parsed {Records.Count} IDB records from {lineNumber} lines");
	}

	private IdbRecord? ParseRecord(string line, int lineNumber)
	{
		// Split by whitespace into tokens
		// The first 7 tokens are positional fields; the rest are attributes
		// However, attributes can contain spaces inside parentheses, so we parse carefully

		int pos = 0;
		var tokens = new List<string>();

		// Read 7 positional fields
		for (int i = 0; i < 7; i++)
		{
			SkipWhitespace(line, ref pos);
			if (pos >= line.Length)
			{
				ValidationReport.Warning($"IDB line {lineNumber}: expected 7 fields, found {i}");
				return null;
			}
			tokens.Add(ReadToken(line, ref pos));
		}

		// Validate type field
		if (tokens[0].Length != 1 || tokens[0][0] < 'a' || tokens[0][0] > 'z')
		{
			ValidationReport.Warning($"IDB line {lineNumber}: invalid type '{tokens[0]}'");
			return null;
		}

		// Parse mode as octal
		UInt32 mode = 0;
		try
		{
			mode = Convert.ToUInt32(tokens[1], 8);
		}
		catch
		{
			ValidationReport.Warning($"IDB line {lineNumber}: invalid octal mode '{tokens[1]}'");
		}

		// Parse attributes (rest of line)
		var attributes = new Dictionary<string, string>();
		while (pos < line.Length)
		{
			SkipWhitespace(line, ref pos);
			if (pos >= line.Length) break;

			string attrName = ReadAttributeName(line, ref pos);
			if (attrName.Length == 0) break;

			if (pos < line.Length && line[pos] == '(')
			{
				// Read value until closing )
				pos++; // skip (
				int valueStart = pos;
				int depth = 1;
				while (pos < line.Length && depth > 0)
				{
					if (line[pos] == '(') depth++;
					else if (line[pos] == ')') depth--;
					if (depth > 0) pos++;
				}
				string value = line[valueStart..pos];
				if (pos < line.Length) pos++; // skip )
				attributes[attrName] = value;
			}
			else
			{
				// Flag attribute (no value)
				attributes[attrName] = "";
			}
		}

		// Validate filename - reject absolute paths and ..
		string fileName = tokens[4];
		if (fileName.StartsWith('/') || fileName.Contains(".."))
		{
			ValidationReport.Warning($"IDB line {lineNumber}: unsafe path '{fileName}'");
			return null;
		}

		return new IdbRecord
		{
			LineNumber = lineNumber,
			Type = tokens[0][0],
			Mode = mode,
			ModeString = tokens[1],
			Owner = tokens[2],
			Group = tokens[3],
			FileName = fileName,
			Source = tokens[5],
			Subsystem = tokens[6],
			Attributes = attributes
		};
	}

	private void ReadImageFile(string directory, string imageName)
	{
		// Build the image file name: <basename>.<imageName>
		string imageFileName = $"{BaseName}.{imageName}";
		string imagePath = Path.Combine(directory, imageFileName);

		if (!File.Exists(imagePath))
		{
			// Try case variations
			string imagePathUpper = Path.Combine(directory, imageFileName.ToUpperInvariant());
			if (File.Exists(imagePathUpper))
				imagePath = imagePathUpper;
			else
			{
				Log.LogInformation($"Image file not found: {imageFileName}");
				ValidationReport.Warning($"Image file not found: {imageFileName}");
				return;
			}
		}

		Log.LogInformation($"Reading image file: {imagePath}");

		using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);

		// Read version banner (scan for first NUL byte)
		var banner = ReadBanner(imageStream, imageName);
		if (banner != null)
		{
			ImageBanners[imageName] = banner;
			Log.LogInformation($"Image '{imageName}' banner: \"{banner.Text}\" ({banner.RawBytes.Length} bytes)");
		}

		// Validate file regions for each f-record referencing this image
		var fileRecords = Records
			.Where(r => r.Type == 'f' && r.ImageName == imageName)
			.OrderBy(r => r.Offset)
			.ToList();

		foreach (var record in fileRecords)
		{
			var region = ValidateFileRegion(imageStream, record, imageFileName);
			if (region != null)
			{
				FileRegions.Add(region);
			}
		}
	}

	private ImageBanner? ReadBanner(Stream stream, string imageName)
	{
		stream.Seek(0, SeekOrigin.Begin);
		var bytes = new List<byte>();
		int b;
		while ((b = stream.ReadByte()) >= 0)
		{
			bytes.Add((byte)b);
			if (b == 0) break;

			// Safety limit on banner length
			if (bytes.Count > 4096)
			{
				ValidationReport.Warning($"Image '{imageName}': banner exceeds 4096 bytes, likely corrupt");
				return null;
			}
		}

		if (bytes.Count == 0)
			return null;

		var rawBytes = bytes.ToArray();
		// Banner text is everything up to (not including) the NUL
		string text = Encoding.ASCII.GetString(rawBytes, 0, rawBytes.Length - 1);
		return new ImageBanner(text, rawBytes, 0);
	}

	private FileRegion? ValidateFileRegion(Stream imageStream, IdbRecord record, string imageFileName)
	{
		long offset = record.Offset;

		// Validate offset is within file
		if (offset < 0 || offset >= imageStream.Length)
		{
			ValidationReport.Warning($"File '{record.FileName}': offset {offset} is outside image file (length {imageStream.Length})");
			return null;
		}

		imageStream.Seek(offset, SeekOrigin.Begin);

		// Read 2-byte big-endian pathname length
		int b1 = imageStream.ReadByte();
		int b2 = imageStream.ReadByte();
		if (b1 < 0 || b2 < 0)
		{
			ValidationReport.Warning($"File '{record.FileName}': could not read pathname length at offset {offset}");
			return null;
		}
		UInt16 pathLen = (UInt16)((b1 << 8) | b2);

		// Read pathname
		byte[] pathBytes = new byte[pathLen];
		int read = imageStream.Read(pathBytes, 0, pathLen);
		if (read < pathLen)
		{
			ValidationReport.Warning($"File '{record.FileName}': could not read full pathname ({read}/{pathLen} bytes)");
			return null;
		}
		string pathname = Encoding.ASCII.GetString(pathBytes);

		// Validate pathname matches IDB record
		bool matches = pathname == record.FileName;
		if (!matches)
		{
			ValidationReport.Warning($"File '{record.FileName}': embedded pathname '{pathname}' does not match IDB filename");
		}

		long payloadOffset = offset + 2 + pathLen;
		long payloadLength = record.PayloadLength;

		// Validate payload doesn't exceed file
		if (payloadOffset + payloadLength > imageStream.Length)
		{
			ValidationReport.Warning($"File '{record.FileName}': payload extends past end of image file ({payloadOffset + payloadLength} > {imageStream.Length})");
			return null;
		}

		return new FileRegion(record, pathLen, pathname, matches, offset, payloadOffset, payloadLength, imageFileName);
	}

	private static void SkipWhitespace(string s, ref int pos)
	{
		while (pos < s.Length && (s[pos] == ' ' || s[pos] == '\t'))
			pos++;
	}

	private static string ReadToken(string s, ref int pos)
	{
		int start = pos;
		while (pos < s.Length && s[pos] != ' ' && s[pos] != '\t')
			pos++;
		return s[start..pos];
	}

	private static string ReadAttributeName(string s, ref int pos)
	{
		int start = pos;
		while (pos < s.Length && s[pos] != ' ' && s[pos] != '\t' && s[pos] != '(')
			pos++;
		return s[start..pos];
	}
}
