namespace Ufex.Extensions.Archive.CAB.Data;

/// <summary>
/// Lookup tables and constant descriptions for CAB format fields
/// </summary>
internal static class Constants
{
	/// <summary>
	/// Compression type descriptions for CFFOLDER.typeCompress
	/// </summary>
	public static string CompressionTypeDescription(UInt16 typeCompress)
	{
		// Low byte is the compression type, high byte is for LZX window size
		UInt16 compType = (UInt16)(typeCompress & 0x000F);
		return compType switch
		{
			0x0000 => "None",
			0x0001 => "MSZIP",
			0x0002 => "Quantum",
			0x0003 => "LZX",
			_ => $"Unknown (0x{typeCompress:X4})"
		};
	}

	/// <summary>
	/// Describes CFHEADER flags
	/// </summary>
	public static string FlagsDescription(UInt16 flags)
	{
		var parts = new List<string>();
		if ((flags & Signatures.CfhdrPrevCabinet) != 0) parts.Add("PREV_CABINET");
		if ((flags & Signatures.CfhdrNextCabinet) != 0) parts.Add("NEXT_CABINET");
		if ((flags & Signatures.CfhdrReservePresent) != 0) parts.Add("RESERVE_PRESENT");
		return parts.Count > 0 ? string.Join(" | ", parts) : "None";
	}

	/// <summary>
	/// Describes CFFILE attribs
	/// </summary>
	public static string AttribsDescription(UInt16 attribs)
	{
		var parts = new List<string>();
		if ((attribs & Signatures.AttrReadOnly) != 0) parts.Add("Read-Only");
		if ((attribs & Signatures.AttrHidden) != 0) parts.Add("Hidden");
		if ((attribs & Signatures.AttrSystem) != 0) parts.Add("System");
		if ((attribs & Signatures.AttrArchive) != 0) parts.Add("Archive");
		if ((attribs & Signatures.AttrExec) != 0) parts.Add("Execute");
		if ((attribs & Signatures.AttrNameIsUtf) != 0) parts.Add("UTF Name");
		return parts.Count > 0 ? string.Join(" | ", parts) : "None";
	}

	/// <summary>
	/// Describes CFFILE iFolder special values
	/// </summary>
	public static string FolderIndexDescription(UInt16 iFolder, UInt16 cFolders)
	{
		return iFolder switch
		{
			Signatures.IfoldContinuedFromPrev => "Continued from previous cabinet",
			Signatures.IfoldContinuedToNext => "Continued to next cabinet",
			Signatures.IfoldContinuedPrevAndNext => "Continued from previous and to next cabinet",
			_ => $"Folder {iFolder}"
		};
	}
}
