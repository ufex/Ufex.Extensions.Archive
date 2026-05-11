using System.IO.Compression;
using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.ISCAB.Data;

namespace Ufex.Extensions.Archive.ISCAB.Structure;

/// <summary>
/// File descriptor node for v5 archives
/// </summary>
internal class FileDescriptorV5Node : SectionNode
{
	public override string Description => $"File: {((FileDescriptorV5)Section).FileName}";

	public FileDescriptorV5Node(FileDescriptorV5 fd)
		: base(fd, fd.FileName.Length > 0 ? fd.FileName : "(unnamed)", TreeViewIcon.Document)
	{
	}

	public override IReadOnlyList<NodeAction> GetActions()
	{
		var d = (FileDescriptorV5)Section;
		bool isCompressed = (d.Flags & Signatures.FileCompressed) != 0;
		bool isSplit = (d.Flags & Signatures.FileSplit) != 0;
		bool hasData = d.CompressedSize > 0;

		bool canExtract = hasData && !isSplit;
		string? disabledReason = null;
		if (isSplit)
			disabledReason = "File is split across volumes";
		else if (!hasData)
			disabledReason = "File is empty";

		return
		[
			new NodeAction
			{
				Id = "extract",
				Label = "Extract...",
				Group = NodeActionGroup.Export,
				IsEnabled = canExtract,
				DisabledReason = disabledReason,
				Inputs =
				[
					new SaveFileInput
					{
						Key = "destination",
						Prompt = "Save extracted file as",
						SuggestedFileName = Path.GetFileName(d.FileName)
					}
				]
			}
		];
	}

	public override ActionResult ExecuteAction(string actionId, ActionContext context)
	{
		if (actionId != "extract")
			return base.ExecuteAction(actionId, context);

		var d = (FileDescriptorV5)Section;
		var destination = context.GetInput<string>("destination");
		var stream = context.FileContext.FileStream;
		bool isCompressed = (d.Flags & Signatures.FileCompressed) != 0;

		stream.Seek(d.DataOffset, SeekOrigin.Begin);

		using var output = File.Create(destination);

		if (!isCompressed)
		{
			CopyBytes(stream, output, d.ExpandedSize, context);
		}
		else
		{
			DecompressChunked(stream, output, d.CompressedSize, context);
		}

		return ActionResult.Success($"Extracted to {destination}");
	}

	/// <summary>
	/// Decompresses ISCAB chunked data. Each chunk is a u16 length prefix
	/// followed by that many bytes of raw deflate data.
	/// </summary>
	private static void DecompressChunked(Stream source, Stream destination, long compressedSize, ActionContext context)
	{
		long endPosition = source.Position + compressedSize;
		byte[] lengthBuf = new byte[2];

		while (source.Position < endPosition)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			// Read u16 chunk length (little-endian)
			if (source.Read(lengthBuf, 0, 2) < 2)
				break;
			int chunkLength = lengthBuf[0] | (lengthBuf[1] << 8);
			if (chunkLength == 0)
				break;

			// Decompress the chunk as raw deflate
			using var deflate = new DeflateStream(
				new BoundedStream(source, chunkLength), CompressionMode.Decompress, leaveOpen: true);
			deflate.CopyTo(destination);
		}
	}

	private static void CopyBytes(Stream source, Stream destination, long count, ActionContext context)
	{
		byte[] buffer = new byte[81920];
		long totalCopied = 0;
		while (totalCopied < count)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			int toRead = (int)Math.Min(buffer.Length, count - totalCopied);
			int read = source.Read(buffer, 0, toRead);
			if (read == 0) break;
			destination.Write(buffer, 0, read);
			totalCopied += read;
		}
	}

	private sealed class BoundedStream : Stream
	{
		private readonly Stream _inner;
		private long _remaining;

		public BoundedStream(Stream inner, long length)
		{
			_inner = inner;
			_remaining = length;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_remaining <= 0) return 0;
			int toRead = (int)Math.Min(count, _remaining);
			int read = _inner.Read(buffer, offset, toRead);
			_remaining -= read;
			return read;
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	public override object[][] GetRows()
	{
		var d = (FileDescriptorV5)Section;
		return [
			["Name Offset", d.NameOffset, d.FileName],
			["Directory Index", d.DirectoryIndex, d.DirectoryName],
			["Unknown", d.Unknown1, $"{d.Unknown1.Length} bytes"],
			["Flags", d.Flags, d.FlagsText],
			["Expanded Size", d.ExpandedSize, ByteCountFormatter.Format(d.ExpandedSize)],
			["Compressed Size", d.CompressedSize, ByteCountFormatter.Format(d.CompressedSize)],
			["Unknown", d.Unknown2, $"{d.Unknown2.Length} bytes"],
			["Data Offset", d.DataOffset, $"0x{d.DataOffset:X8}"],
			["MD5", d.Md5, BitConverter.ToString(d.Md5).Replace("-", "")],
		];
	}
}
