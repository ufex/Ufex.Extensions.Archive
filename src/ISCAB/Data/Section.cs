namespace Ufex.Extensions.Archive.ISCAB.Data;

/// <summary>
/// Base class for all ISCAB file sections
/// </summary>
public abstract class Section
{
	public long StartPosition { get; protected set; }
	public long EndPosition { get; protected set; }

	protected void StartRead(BinaryReader br)
	{
		StartPosition = br.BaseStream.Position;
	}

	protected void EndRead(BinaryReader br)
	{
		EndPosition = br.BaseStream.Position;
	}
}
