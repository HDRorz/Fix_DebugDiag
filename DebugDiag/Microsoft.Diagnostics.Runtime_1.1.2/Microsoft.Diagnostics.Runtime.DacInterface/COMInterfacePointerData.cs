namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct COMInterfacePointerData
{
	public readonly ulong MethodTable;

	public readonly ulong InterfacePointer;

	public readonly ulong ComContext;
}
