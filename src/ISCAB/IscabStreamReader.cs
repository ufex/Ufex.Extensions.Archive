using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB;

public class IscabStreamReader
{
	private Stream _primaryStream;
	private Stream? _hdrStream;

	public CommonHeader? CabCommonHeader { get; private set; }
	public VolumeHeader? CabVolumeHeader { get; private set; }

	public CommonHeader? HdrCommonHeader { get; private set; }
	public CabDescriptor? Descriptor { get; private set; }

	public List<string> Directories { get; init; }
	public List<FileDescriptorV5> FilesV5 { get; init; }
	public List<FileDescriptorV6> FilesV6 { get; init; }
	public List<FileGroupDescriptor> FileGroups { get; init; }
	public List<ComponentDescriptor> Components { get; init; }

	public Int32 MajorVersion { get; private set; }
	public bool HasHdrFile => _hdrStream != null;

	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	/// <summary>
	/// Creates a reader for an ISCAB archive.
	/// </summary>
	/// <param name="primaryStream">The main file stream (the .cab file opened by the user)</param>
	/// <param name="hdrStream">Optional .hdr file stream if found alongside the .cab</param>
	/// <param name="log">Logger</param>
	/// <param name="validationReport">Validation report</param>
	public IscabStreamReader(Stream primaryStream, Stream? hdrStream, Logger log, ValidationReport validationReport)
	{
		_primaryStream = primaryStream;
		_hdrStream = hdrStream;
		Directories = new List<string>();
		FilesV5 = new List<FileDescriptorV5>();
		FilesV6 = new List<FileDescriptorV6>();
		FileGroups = new List<FileGroupDescriptor>();
		Components = new List<ComponentDescriptor>();
		Log = log;
		ValidationReport = validationReport;
	}

	public bool Read()
	{
		// Read the CAB file common header
		FileReader cabReader = new FileReader(_primaryStream, Endian.Little);
		CabCommonHeader = new CommonHeader(cabReader);

		if (CabCommonHeader.Signature != Signatures.CabSignature)
		{
			ValidationReport.Error($"Invalid ISCAB signature: 0x{CabCommonHeader.Signature:X8}");
			return false;
		}

		MajorVersion = CabCommonHeader.MajorVersion;
		Log.LogInformation($"ISCAB cab file: version=0x{CabCommonHeader.Version:X8}, major={MajorVersion}");

		// Read volume header from cab
		CabVolumeHeader = new VolumeHeader(cabReader, MajorVersion >= 6);
		Log.LogInformation($"Volume header: firstFile={CabVolumeHeader.FirstFileIndex}, lastFile={CabVolumeHeader.LastFileIndex}");

		// Read metadata from HDR file if available, otherwise from cab
		Stream metadataStream;
		if (_hdrStream != null)
		{
			metadataStream = _hdrStream;
			FileReader hdrReader = new FileReader(metadataStream, Endian.Little);
			HdrCommonHeader = new CommonHeader(hdrReader);

			if (HdrCommonHeader.Signature != Signatures.CabSignature)
			{
				ValidationReport.Error($"Invalid HDR file signature: 0x{HdrCommonHeader.Signature:X8}");
				return false;
			}

			Log.LogInformation($"ISCAB hdr file: version=0x{HdrCommonHeader.Version:X8}, major={HdrCommonHeader.MajorVersion}");
			ValidationReport.Info("Header file (.hdr) loaded successfully");
		}
		else
		{
			metadataStream = _primaryStream;
			HdrCommonHeader = CabCommonHeader;
			ValidationReport.Info("No .hdr file found; reading metadata from .cab");
		}

		// Read CAB Descriptor from the metadata source
		CommonHeader metaHeader = HasHdrFile ? HdrCommonHeader! : CabCommonHeader;
		long metaLength = metadataStream.Length;

		if (metaHeader.CabDescriptorSize == 0)
		{
			ValidationReport.Error("CAB descriptor size is 0; no metadata available");
			return false;
		}

		if (metaHeader.CabDescriptorOffset >= (ulong)metaLength)
		{
			ValidationReport.Error($"CAB descriptor offset (0x{metaHeader.CabDescriptorOffset:X}) is beyond end of file (0x{metaLength:X})");
			return false;
		}

		if (metaHeader.CabDescriptorOffset + CabDescriptor.FixedSize > (ulong)metaLength)
		{
			ValidationReport.Error($"CAB descriptor at offset 0x{metaHeader.CabDescriptorOffset:X} ({CabDescriptor.FixedSize} bytes) extends beyond end of file (0x{metaLength:X})");
			return false;
		}

		FileReader metaReader = new FileReader(metadataStream, Endian.Little);
		metadataStream.Position = metaHeader.CabDescriptorOffset;
		Descriptor = new CabDescriptor(metaReader);
		Log.LogInformation($"CAB Descriptor: {Descriptor.DirectoryCount} dirs, {Descriptor.FileCount} files");

		// Validate descriptor counts
		if (Descriptor.DirectoryCount > 10000)
		{
			ValidationReport.Error($"Unreasonable directory count: {Descriptor.DirectoryCount}");
			return false;
		}
		if (Descriptor.FileCount > 100000)
		{
			ValidationReport.Error($"Unreasonable file count: {Descriptor.FileCount}");
			return false;
		}

		long fileTableAbsolute = metaHeader.CabDescriptorOffset + Descriptor.FileTableOffset;
		if (fileTableAbsolute >= metaLength)
		{
			ValidationReport.Error($"File table offset (0x{fileTableAbsolute:X}) is beyond end of file (0x{metaLength:X})");
			return false;
		}

		// Parse directories
		ReadDirectories(metaReader, metaHeader, metaLength);

		// Parse file descriptors
		if (MajorVersion >= 6)
		{
			ReadFileDescriptorsV6(metaReader, metaHeader, metaLength);
		}
		else
		{
			ReadFileDescriptorsV5(metaReader, metaHeader, metaLength);
		}

		// Parse file groups and components
		ReadFileGroups(metaReader, metaHeader, metaLength);
		ReadComponents(metaReader, metaHeader, metaLength);

		ValidationReport.Info($"Parsed ISCAB: version {MajorVersion}, {Directories.Count} directories, {(MajorVersion >= 6 ? FilesV6.Count : FilesV5.Count)} files, {FileGroups.Count} file groups, {Components.Count} components");
		return true;
	}

	private void ReadDirectories(FileReader fr, CommonHeader header, long streamLength)
	{
		long baseOffset = header.CabDescriptorOffset;
		long fileTableAbsolute = baseOffset + Descriptor!.FileTableOffset;

		for (int i = 0; i < (int)Descriptor.DirectoryCount; i++)
		{
			long indexPos = fileTableAbsolute + i * 4;
			if (indexPos + 4 > streamLength)
			{
				ValidationReport.Error($"Directory index {i} offset table entry (0x{indexPos:X}) is beyond end of file");
				break;
			}
			fr.BaseStream.Position = indexPos;
			UInt32 dirNameOffset = fr.ReadUInt32();

			// Directory name offsets are relative to file_table_offset
			long nameAbsolute = fileTableAbsolute + dirNameOffset;
			if (nameAbsolute >= streamLength)
			{
				ValidationReport.Error($"Directory {i} name offset (0x{nameAbsolute:X}) is beyond end of file");
				Directories.Add($"<invalid offset 0x{dirNameOffset:X}>");
				continue;
			}
			fr.BaseStream.Position = nameAbsolute;
			string dirName = ReadString(fr, MajorVersion >= 17, streamLength);
			Directories.Add(dirName);
			Log.LogInformation($"Directory {i}: {dirName}");
		}
	}

	private void ReadFileDescriptorsV5(FileReader fr, CommonHeader header, long streamLength)
	{
		long baseOffset = header.CabDescriptorOffset;
		long fileTableAbsolute = baseOffset + Descriptor!.FileTableOffset;

		for (int i = 0; i < (int)Descriptor.FileCount; i++)
		{
			// Read file descriptor offset from file table
			long indexPos = fileTableAbsolute + ((int)Descriptor.DirectoryCount + i) * 4;
			if (indexPos + 4 > streamLength)
			{
				ValidationReport.Error($"File descriptor {i} offset table entry (0x{indexPos:X}) is beyond end of file");
				break;
			}
			fr.BaseStream.Position = indexPos;
			UInt32 fdOffset = fr.ReadUInt32();

			// File descriptor is at file_table_offset + fdOffset (descriptor-relative)
			long fdAbsolute = fileTableAbsolute + fdOffset;
			if (fdAbsolute + Constants.FileDescriptorV5Size > streamLength)
			{
				ValidationReport.Error($"File descriptor {i} at offset 0x{fdAbsolute:X} ({Constants.FileDescriptorV5Size} bytes) extends beyond end of file");
				break;
			}
			fr.BaseStream.Position = fdAbsolute;
			var fd = new FileDescriptorV5(fr);

			// Resolve file name
			if (fd.NameOffset != 0)
			{
				long nameAbsolute = fileTableAbsolute + fd.NameOffset;
				if (nameAbsolute >= streamLength)
				{
					ValidationReport.Warning($"File descriptor {i} name offset (0x{nameAbsolute:X}) is beyond end of file");
				}
				else
				{
					fr.BaseStream.Position = nameAbsolute;
					fd.FileName = ReadString(fr, MajorVersion >= 17, streamLength);
				}
			}

			// Resolve directory name
			if (fd.DirectoryIndex < Directories.Count)
			{
				fd.DirectoryName = Directories[fd.DirectoryIndex];
			}

			FilesV5.Add(fd);
		}
		Log.LogInformation($"Read {FilesV5.Count} v5 file descriptors");
	}

	private void ReadFileDescriptorsV6(FileReader fr, CommonHeader header, long streamLength)
	{
		long baseOffset = header.CabDescriptorOffset;
		long fileTableAbsolute = baseOffset + Descriptor!.FileTableOffset;
		long descriptorArrayStart = fileTableAbsolute + Descriptor.FileTableOffset2;

		for (int i = 0; i < (int)Descriptor.FileCount; i++)
		{
			long fdAbsolute = descriptorArrayStart + i * Constants.FileDescriptorV6Size;
			if (fdAbsolute + Constants.FileDescriptorV6Size > streamLength)
			{
				ValidationReport.Error($"File descriptor {i} at offset 0x{fdAbsolute:X} ({Constants.FileDescriptorV6Size} bytes) extends beyond end of file");
				break;
			}
			fr.BaseStream.Position = fdAbsolute;
			var fd = new FileDescriptorV6(fr);

			// Resolve file name
			if (fd.NameOffset != 0)
			{
				long nameAbsolute = fileTableAbsolute + fd.NameOffset;
				if (nameAbsolute >= streamLength)
				{
					ValidationReport.Warning($"File descriptor {i} name offset (0x{nameAbsolute:X}) is beyond end of file");
				}
				else
				{
					fr.BaseStream.Position = nameAbsolute;
					fd.FileName = ReadString(fr, MajorVersion >= 17, streamLength);
				}
			}

			// Resolve directory name
			if (fd.DirectoryIndex < Directories.Count)
			{
				fd.DirectoryName = Directories[fd.DirectoryIndex];
			}

			FilesV6.Add(fd);
		}
		Log.LogInformation($"Read {FilesV6.Count} v6+ file descriptors");
	}

	private void ReadFileGroups(FileReader fr, CommonHeader header, long streamLength)
	{
		long baseOffset = header.CabDescriptorOffset;
		long fileTableAbsolute = baseOffset + Descriptor!.FileTableOffset;

		foreach (UInt32 slot in Descriptor.FileGroupOffsets)
		{
			if (slot == 0) continue;

			UInt32 currentOffset = slot;
			int safety = 0;
			while (currentOffset != 0 && safety < 1024)
			{
				long nodePos = baseOffset + currentOffset;
				if (nodePos + 12 > streamLength)
				{
					ValidationReport.Warning($"File group linked list node at 0x{nodePos:X} is beyond end of file");
					break;
				}
				fr.BaseStream.Position = nodePos;
				var node = new OffsetListNode(fr);

				// Read the file group descriptor
				long descPos = baseOffset + node.DescriptorOffset;
				if (descPos >= streamLength)
				{
					ValidationReport.Warning($"File group descriptor at 0x{descPos:X} is beyond end of file");
					break;
				}
				fr.BaseStream.Position = descPos;
				var fg = new FileGroupDescriptor(fr, MajorVersion);

				// Resolve name
				if (node.NameOffset != 0)
				{
					long namePos = baseOffset + node.NameOffset;
					if (namePos >= streamLength)
					{
						ValidationReport.Warning($"File group name at 0x{namePos:X} is beyond end of file");
					}
					else
					{
						fr.BaseStream.Position = namePos;
						fg.Name = ReadString(fr, MajorVersion >= 17, streamLength);
					}
				}

				FileGroups.Add(fg);
				currentOffset = node.NextOffset;
				safety++;
			}
		}
		Log.LogInformation($"Read {FileGroups.Count} file groups");
	}

	private void ReadComponents(FileReader fr, CommonHeader header, long streamLength)
	{
		long baseOffset = header.CabDescriptorOffset;

		foreach (UInt32 slot in Descriptor!.ComponentOffsets)
		{
			if (slot == 0) continue;

			UInt32 currentOffset = slot;
			int safety = 0;
			while (currentOffset != 0 && safety < 1024)
			{
				long nodePos = baseOffset + currentOffset;
				if (nodePos + 12 > streamLength)
				{
					ValidationReport.Warning($"Component linked list node at 0x{nodePos:X} is beyond end of file");
					break;
				}
				fr.BaseStream.Position = nodePos;
				var node = new OffsetListNode(fr);

				// Read the component descriptor
				long descPos = baseOffset + node.DescriptorOffset;
				if (descPos >= streamLength)
				{
					ValidationReport.Warning($"Component descriptor at 0x{descPos:X} is beyond end of file");
					break;
				}
				fr.BaseStream.Position = descPos;
				var comp = new ComponentDescriptor(fr, MajorVersion);

				// Resolve name
				if (node.NameOffset != 0)
				{
					long namePos = baseOffset + node.NameOffset;
					if (namePos >= streamLength)
					{
						ValidationReport.Warning($"Component name at 0x{namePos:X} is beyond end of file");
					}
					else
					{
						fr.BaseStream.Position = namePos;
						comp.Name = ReadString(fr, MajorVersion >= 17, streamLength);
					}
				}

				Components.Add(comp);
				currentOffset = node.NextOffset;
				safety++;
			}
		}
		Log.LogInformation($"Read {Components.Count} components");
	}

	private static string ReadString(FileReader fr, bool isUtf16, long streamLength)
	{
		if (isUtf16)
		{
			var chars = new List<byte>();
			while (fr.BaseStream.Position + 1 < streamLength)
			{
				byte lo = fr.ReadByte();
				byte hi = fr.ReadByte();
				if (lo == 0 && hi == 0) break;
				chars.Add(lo);
				chars.Add(hi);
			}
			return Encoding.Unicode.GetString(chars.ToArray());
		}
		else
		{
			var bytes = new List<byte>();
			while (fr.BaseStream.Position < streamLength)
			{
				byte b = fr.ReadByte();
				if (b == 0) break;
				bytes.Add(b);
			}
			return Encoding.ASCII.GetString(bytes.ToArray());
		}
	}
}
