namespace Microsoft.Diagnostics.Runtime;

public abstract class ManagedWorkItem
{
	public abstract ulong Object { get; }

	public abstract ClrType Type { get; }
}
