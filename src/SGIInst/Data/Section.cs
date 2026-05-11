namespace Ufex.Extensions.Archive.SGIInst.Data;

/// <summary>
/// Base class for all SGI inst sections
/// </summary>
public abstract class Section
{
	public long StartPosition { get; protected set; }
	public long EndPosition { get; protected set; }

	protected void StartRead(long position)
	{
		StartPosition = position;
	}

	protected void EndRead(long position)
	{
		EndPosition = position;
	}
}
