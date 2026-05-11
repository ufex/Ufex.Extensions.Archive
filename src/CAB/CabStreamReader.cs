using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Archive.CAB.Data;

namespace Ufex.Extensions.Archive.CAB;

public class CabStreamReader
{
	private Stream _fileStream;

	public CfHeader? Header { get; private set; }
	public List<CfFolder> Folders { get; init; }
	public List<CfFile> Files { get; init; }
	public List<CfData> DataBlocks { get; init; }

	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	public CabStreamReader(Stream fileStream, Logger log, ValidationReport validationReport)
	{
		_fileStream = fileStream;
		Folders = new List<CfFolder>();
		Files = new List<CfFile>();
		DataBlocks = new List<CfData>();
		Log = log;
		ValidationReport = validationReport;
	}

	public bool Read()
	{
		FileReader fr = new FileReader(_fileStream, Endian.Little);

		// 1. Read CFHEADER
		Header = new CfHeader(fr);
		Log.LogInformation($"Read CFHEADER: version {Header.VersionText}, {Header.CFolders} folders, {Header.CFiles} files, cabinet size {Header.CbCabinet}");

		// Validate signature
		if (!Header.Signature.SequenceEqual(Signatures.FileSignature))
		{
			ValidationReport.Error("Invalid cabinet file signature");
			return false;
		}

		ValidationReport.Info($"Cabinet version {Header.VersionText}");

		// 2. Read CFFOLDER entries
		byte cbCFFolder = Header.HasReserve ? Header.CbCFFolder : (byte)0;
		for (int i = 0; i < Header.CFolders; i++)
		{
			var folder = new CfFolder(fr, cbCFFolder);
			Folders.Add(folder);
			Log.LogInformation($"Read CFFOLDER {i}: offset={folder.CoffCabStart}, dataBlocks={folder.CCFData}, compression={folder.TypeCompressText}");
		}

		// 3. Read CFFILE entries (starting at coffFiles offset)
		_fileStream.Position = Header.CoffFiles;
		for (int i = 0; i < Header.CFiles; i++)
		{
			var file = new CfFile(fr);
			Files.Add(file);
			Log.LogInformation($"Read CFFILE {i}: name={file.SzName}, size={file.CbFile}, folder={file.IFolder}");
		}

		// 4. Read CFDATA blocks for each folder
		byte cbCFData = Header.HasReserve ? Header.CbCFData : (byte)0;
		foreach (var folder in Folders)
		{
			_fileStream.Position = folder.CoffCabStart;
			for (int i = 0; i < folder.CCFData; i++)
			{
				var dataBlock = new CfData(fr, cbCFData);
				DataBlocks.Add(dataBlock);
			}
		}

		ValidationReport.Info($"Successfully parsed cabinet: {Header.CFolders} folders, {Header.CFiles} files, {DataBlocks.Count} data blocks");
		return true;
	}
}
