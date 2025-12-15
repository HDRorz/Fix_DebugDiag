namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct COMInterfacePointerData
{
	public ulong MethodTable;

	public ulong InterfacePtr;

	public ulong ComContext;
}
