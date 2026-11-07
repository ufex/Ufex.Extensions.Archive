namespace Ufex.Extensions.Archive.SGIInst;

/// <summary>
/// Decompresses data in the BSD compress(1) format (adaptive LZW, magic 0x1F 0x9D).
/// This is the exact format used by the IRIX inst packaging system for file payloads.
/// </summary>
public static class LzwDecompressor
{
	/// <summary>
	/// Decompresses a BSD compress stream.
	/// </summary>
	/// <param name="input">Stream positioned at the start of the compressed data (0x1F 0x9D header).</param>
	/// <param name="compressedLength">Number of compressed bytes to read.</param>
	/// <param name="output">Stream to write decompressed data to.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of bytes written to output.</returns>
	public static long Decompress(Stream input, long compressedLength, Stream output, CancellationToken cancellationToken = default)
	{
		// Read the 3-byte header
		int magic1 = input.ReadByte();
		int magic2 = input.ReadByte();
		int flags = input.ReadByte();

		if (magic1 != 0x1F || magic2 != 0x9D)
			throw new InvalidDataException($"Not a BSD compress stream (magic: 0x{magic1:X2} 0x{magic2:X2})");

		int maxBits = flags & 0x1F;
		bool blockMode = (flags & 0x80) != 0;

		if (maxBits < 9 || maxBits > 24)
			throw new InvalidDataException($"Invalid max bits: {maxBits}");

		int maxCode = (1 << maxBits) - 1;

		// Read all compressed bytes into a buffer for bit-level access
		long remaining = compressedLength - 3;
		byte[] compressedData = new byte[remaining];
		int totalRead = 0;
		while (totalRead < remaining)
		{
			int read = input.Read(compressedData, totalRead, (int)(remaining - totalRead));
			if (read == 0) break;
			totalRead += read;
		}

		// LZW decompression state
		// Dictionary: each entry is (prefix code, suffix byte)
		// For codes 0-255, prefix = -1, suffix = code
		int dictSize = blockMode ? 257 : 256; // 256 = CLEAR code when blockMode
		int[] prefix = new int[maxCode + 1];
		byte[] suffix = new byte[maxCode + 1];

		// Initialize single-byte entries
		for (int i = 0; i < 256; i++)
		{
			prefix[i] = -1;
			suffix[i] = (byte)i;
		}
		if (blockMode)
		{
			prefix[256] = -1; // CLEAR code placeholder
			suffix[256] = 0;
		}

		int codeSize = 9;
		int nextFree = dictSize;

		// Bit reading state - codes are packed LSB-first in groups aligned to codeSize boundaries
		int bitPos = 0;
		int totalBits = totalRead * 8;

		// Output buffer for building strings from dictionary chains
		byte[] decodeStack = new byte[1 << maxBits];
		byte[] writeBuffer = new byte[81920];
		int writePos = 0;
		long totalWritten = 0;

		int prevCode = -1;
		byte firstByte = 0;

		while (bitPos < totalBits)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// BSD compress packs codes in groups of n_bits codes.
			// Each group is aligned to a byte boundary at the start.
			// Within a group, codes are packed LSB-first.
			int code = ReadCode(compressedData, ref bitPos, codeSize, totalBits);
			if (code < 0) break;

			// Handle CLEAR code in block mode
			if (blockMode && code == 256)
			{
				// Reset dictionary
				dictSize = 257;
				nextFree = 257;
				codeSize = 9;

				// Skip to next code-size boundary in the bit stream
				// BSD compress aligns to n_bits byte boundaries after CLEAR
				int bitsInGroup = bitPos % (codeSize * 8);
				if (bitsInGroup != 0)
					bitPos += (codeSize * 8) - bitsInGroup;

				prevCode = -1;
				continue;
			}

			if (code > nextFree)
				throw new InvalidDataException($"Invalid LZW code {code} (next free: {nextFree})");

			int curCode = code;
			int stackPos = 0;

			// Special case: code == nextFree (the KwKwK case)
			if (code == nextFree)
			{
				if (prevCode < 0)
					throw new InvalidDataException("KwKwK case with no previous code");
				decodeStack[stackPos++] = firstByte;
				curCode = prevCode;
			}

			// Chase the dictionary chain to decode the string
			while (curCode >= 256)
			{
				if (stackPos >= decodeStack.Length)
					throw new InvalidDataException("Dictionary chain too long");
				decodeStack[stackPos++] = suffix[curCode];
				curCode = prefix[curCode];
			}
			decodeStack[stackPos++] = suffix[curCode];
			firstByte = suffix[curCode];

			// Write the decoded string (it's in reverse order on the stack)
			for (int i = stackPos - 1; i >= 0; i--)
			{
				writeBuffer[writePos++] = decodeStack[i];
				if (writePos >= writeBuffer.Length)
				{
					output.Write(writeBuffer, 0, writePos);
					totalWritten += writePos;
					writePos = 0;
				}
			}

			// Add new dictionary entry: prevCode + firstByte
			if (prevCode >= 0 && nextFree <= maxCode)
			{
				prefix[nextFree] = prevCode;
				suffix[nextFree] = firstByte;
				nextFree++;

				// Increase code size when we've used all codes at current size
				if (nextFree > (1 << codeSize) - 1 && codeSize < maxBits)
				{
					codeSize++;
				}
			}

			prevCode = code;
		}

		// Flush remaining output
		if (writePos > 0)
		{
			output.Write(writeBuffer, 0, writePos);
			totalWritten += writePos;
		}

		return totalWritten;
	}

	/// <summary>
	/// Reads one LZW code of the given bit width from the compressed data.
	/// Codes are packed LSB-first within each byte.
	/// </summary>
	private static int ReadCode(byte[] data, ref int bitPos, int codeSize, int totalBits)
	{
		if (bitPos + codeSize > totalBits)
			return -1;

		int code = 0;
		int bitsRead = 0;
		int pos = bitPos;

		while (bitsRead < codeSize)
		{
			int byteIndex = pos / 8;
			int bitIndex = pos % 8;

			if (byteIndex >= data.Length)
				return -1;

			int bitsAvailable = 8 - bitIndex;
			int bitsNeeded = codeSize - bitsRead;
			int bitsToRead = Math.Min(bitsAvailable, bitsNeeded);

			int mask = (1 << bitsToRead) - 1;
			int bits = (data[byteIndex] >> bitIndex) & mask;
			code |= bits << bitsRead;

			bitsRead += bitsToRead;
			pos += bitsToRead;
		}

		bitPos = pos;
		return code;
	}
}
