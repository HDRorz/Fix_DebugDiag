namespace Microsoft.Diagnostics.Runtime;

public abstract class ComInterfaceData
{
	public abstract ClrType Type { get; }

	public abstract ulong InterfacePointer { get; }
}
