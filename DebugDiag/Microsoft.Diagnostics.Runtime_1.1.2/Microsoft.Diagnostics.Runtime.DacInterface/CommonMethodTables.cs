namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct CommonMethodTables
{
	public readonly ulong ArrayMethodTable;

	public readonly ulong StringMethodTable;

	public readonly ulong ObjectMethodTable;

	public readonly ulong ExceptionMethodTable;

	public readonly ulong FreeMethodTable;

	internal bool Validate()
	{
		if (ArrayMethodTable != 0L && StringMethodTable != 0L && ObjectMethodTable != 0L && ExceptionMethodTable != 0L)
		{
			return FreeMethodTable != 0;
		}
		return false;
	}
}
