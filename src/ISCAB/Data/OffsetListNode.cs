using Ufex.API;

namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// OffsetList linked-list node (12 bytes) used for components and file groups
/// </summary>
public class OffsetListNode : Section
{
	public UInt32 NameOffset { get; init; }
	public UInt32 DescriptorOffset { get; init; }
	public UInt32 NextOffset { get; init; }

	public OffsetListNode(FileReader fr)
	{
		StartRead(fr);
		NameOffset = fr.ReadUInt32();
		DescriptorOffset = fr.ReadUInt32();
		NextOffset = fr.ReadUInt32();
		EndRead(fr);
	}
}
