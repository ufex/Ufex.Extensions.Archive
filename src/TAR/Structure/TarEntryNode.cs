using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Format;
using Ufex.Extensions.Archive.TAR.Data;

namespace Ufex.Extensions.Archive.TAR.Structure;

/// <summary>
/// TAR entry node showing the full header details
/// </summary>
internal class TarEntryNode : SectionNode
{
	public override string Description
	{
		get
		{
			var entry = (TarEntry)Section;
			var typeDesc = entry.Header.TypeFlagText;
			return $"{typeDesc}: {entry.DisplayName}";
		}
	}

	public TarEntryNode(TarEntry entry)
		: base(entry, GetNodeText(entry), GetNodeIcon(entry))
	{
	}

	private static string GetNodeText(TarEntry entry)
	{
		return entry.DisplayName.Length > 0 ? entry.DisplayName : "(unnamed)";
	}

	private static TreeViewIcon GetNodeIcon(TarEntry entry)
	{
		return entry.Header.TypeFlagChar switch
		{
			'5' => TreeViewIcon.FolderClosed,
			'2' => TreeViewIcon.Object,
			'1' => TreeViewIcon.Object,
			_ => TreeViewIcon.Document
		};
	}

	public override IReadOnlyList<NodeAction> GetActions()
	{
		var entry = (TarEntry)Section;
		var typeFlagChar = entry.Header.TypeFlagChar;
		bool isFile = typeFlagChar == '\0' || typeFlagChar == '0' || typeFlagChar == '7';
		bool hasData = entry.Header.Size > 0;

		if (!isFile)
			return [];

		return
		[
			new NodeAction
			{
				Id = "extract",
				Label = "Extract...",
				Group = NodeActionGroup.Export,
				IsEnabled = hasData,
				DisabledReason = !hasData ? "File is empty" : null,
				Inputs =
				[
					new SaveFileInput
					{
						Key = "destination",
						Prompt = "Save extracted file as",
						SuggestedFileName = Path.GetFileName(entry.DisplayName.TrimEnd('/'))
					}
				]
			}
		];
	}

	public override ActionResult ExecuteAction(string actionId, ActionContext context)
	{
		if (actionId != "extract")
			return base.ExecuteAction(actionId, context);

		var entry = (TarEntry)Section;
		var destination = context.GetInput<string>("destination");
		var stream = context.FileContext.FileStream;
		var size = (long)entry.Header.Size;

		stream.Seek(entry.Header.DataOffset, SeekOrigin.Begin);

		using var output = File.Create(destination);
		byte[] buffer = new byte[81920];
		long remaining = size;
		while (remaining > 0)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			int toRead = (int)Math.Min(buffer.Length, remaining);
			int read = stream.Read(buffer, 0, toRead);
			if (read == 0) break;
			output.Write(buffer, 0, read);
			remaining -= read;
		}

		return ActionResult.Success($"Extracted to {destination}");
	}

	public override object[][] GetRows()
	{
		var e = (TarEntry)Section;
		var h = e.Header;
		var rows = new List<object[]>();

		rows.Add(["Name", h.NameBytes, h.Name]);
		rows.Add(["Mode", h.ModeBytes, $"{h.Mode:o} ({h.ModeText})"]);
		rows.Add(["UID", h.UidBytes, h.Uid.ToString()]);
		rows.Add(["GID", h.GidBytes, h.Gid.ToString()]);
		rows.Add(["Size", h.SizeBytes, ByteCountFormatter.Format((UInt32)Math.Min(h.Size, UInt32.MaxValue))]);
		rows.Add(["Modification Time", h.MtimeBytes, h.MtimeText]);
		rows.Add(["Checksum", h.ChecksumBytes, h.IsChecksumValid ? $"{h.Checksum} (valid)" : $"{h.Checksum} (INVALID, expected {h.ComputedChecksum})"]);
		rows.Add(["Type Flag", h.TypeFlag, h.TypeFlagText]);
		rows.Add(["Link Name", h.LinkNameBytes, e.DisplayLinkName]);

		if (h.IsUstar || h.IsGnu)
		{
			rows.Add(["Magic", h.MagicBytes, h.Magic]);
			rows.Add(["Version", h.VersionBytes, h.Version]);
			rows.Add(["User Name", h.UnameBytes, h.Uname]);
			rows.Add(["Group Name", h.GnameBytes, h.Gname]);
			rows.Add(["Dev Major", h.DevMajorBytes, h.DevMajor.ToString()]);
			rows.Add(["Dev Minor", h.DevMinorBytes, h.DevMinor.ToString()]);
			rows.Add(["Prefix", h.PrefixBytes, h.Prefix]);
		}

		if (e.LongName != null)
		{
			rows.Add(["GNU Long Name", e.LongName.ToCharArray(), e.LongName]);
		}
		if (e.LongLinkName != null)
		{
			rows.Add(["GNU Long Link Name", e.LongLinkName.ToCharArray(), e.LongLinkName]);
		}

		return rows.ToArray();
	}
}
