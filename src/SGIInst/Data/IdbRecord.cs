namespace Ufex.Extensions.Archive.SGIInst.Data;

/// <summary>
/// Represents one parsed record (line) from an .idb file.
/// Each record describes a filesystem entry to be installed.
/// </summary>
public class IdbRecord
{
	/// <summary>
	/// Line number in the .idb file (1-based)
	/// </summary>
	public Int32 LineNumber { get; init; }

	/// <summary>
	/// Filesystem entry type: 'f' (file), 'd' (dir), 'l' (symlink), 'b', 'c', 'p'
	/// </summary>
	public char Type { get; init; }

	/// <summary>
	/// POSIX permission bits (parsed from octal)
	/// </summary>
	public UInt32 Mode { get; init; }

	/// <summary>
	/// Raw octal mode string as it appears in the IDB
	/// </summary>
	public string ModeString { get; init; } = "";

	/// <summary>
	/// Owner user name (e.g. "root")
	/// </summary>
	public string Owner { get; init; } = "";

	/// <summary>
	/// Group name (e.g. "sys")
	/// </summary>
	public string Group { get; init; } = "";

	/// <summary>
	/// Relative install path (no leading /)
	/// </summary>
	public string FileName { get; init; } = "";

	/// <summary>
	/// Build-tree source path (informational, often "?" or "-")
	/// </summary>
	public string Source { get; init; } = "";

	/// <summary>
	/// Three-part dotted subsystem name: product.image.subsystem
	/// </summary>
	public string Subsystem { get; init; } = "";

	/// <summary>
	/// The image filename derived from the first two components of the subsystem
	/// (e.g. "ultra.dev" from "ultra.dev.base", "eoe.sw" from "eoe.sw.unix")
	/// </summary>
	public string ImageName
	{
		get
		{
			var parts = Subsystem.Split('.');
			return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : Subsystem;
		}
	}

	/// <summary>
	/// All parsed attributes (key -> value, flags have empty string value)
	/// </summary>
	public Dictionary<string, string> Attributes { get; init; } = new();

	/// <summary>
	/// Computed offset for records missing the off() attribute.
	/// Set by the stream reader when computing sequential offsets.
	/// </summary>
	public Int64? ComputedOffset { get; set; }

	// --- Convenience accessors for common attributes ---

	/// <summary>
	/// Whether this record has an explicit off() attribute
	/// </summary>
	public bool HasExplicitOffset => Attributes.ContainsKey("off");

	/// <summary>
	/// Byte offset in the image file where this file's region begins.
	/// Uses explicit off() if present, otherwise falls back to computed offset.
	/// </summary>
	public Int64 Offset
	{
		get
		{
			if (Attributes.TryGetValue("off", out var val) && Int64.TryParse(val, out var result))
				return result;
			return ComputedOffset ?? -1;
		}
	}

	/// <summary>
	/// Uncompressed size of the file payload
	/// </summary>
	public Int64 Size => GetInt64Attribute("size");

	/// <summary>
	/// Compressed size; 0 means uncompressed
	/// </summary>
	public Int64 CompressedSize => GetInt64Attribute("cmpsize");

	/// <summary>
	/// BSD sum checksum of uncompressed contents
	/// </summary>
	public Int64 Checksum => GetInt64Attribute("sum");

	/// <summary>
	/// Symbolic link target for type 'l'
	/// </summary>
	public string? SymlinkValue => Attributes.TryGetValue("symval", out var v) ? v : null;

	/// <summary>
	/// Architecture predicate expression
	/// </summary>
	public string? MachPredicate => Attributes.TryGetValue("mach", out var v) ? v : null;

	/// <summary>
	/// Whether the file is marked as a config file
	/// </summary>
	public bool IsConfig => Attributes.ContainsKey("config");

	/// <summary>
	/// Whether the payload is compressed (cmpsize > 0)
	/// </summary>
	public bool IsCompressed => CompressedSize > 0;

	/// <summary>
	/// The on-disk payload length in the image file
	/// </summary>
	public Int64 PayloadLength => IsCompressed ? CompressedSize : Size;

	/// <summary>
	/// Type description text
	/// </summary>
	public string TypeDescription => Constants.EntryTypeDescription(Type);

	/// <summary>
	/// Mode description text
	/// </summary>
	public string ModeDescription => Constants.ModeDescription(Mode);

	private Int64 GetInt64Attribute(string name)
	{
		if (Attributes.TryGetValue(name, out var val) && Int64.TryParse(val, out var result))
			return result;
		return 0;
	}
}
