namespace Ufex.Extensions.Archive.TAR.Data;

/// <summary>
/// Base class for all TAR record sections
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
