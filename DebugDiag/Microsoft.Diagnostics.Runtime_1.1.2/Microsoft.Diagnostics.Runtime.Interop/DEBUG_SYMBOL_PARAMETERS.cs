namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_SYMBOL_PARAMETERS
{
	public ulong Module;

	public uint TypeId;

	public uint ParentSymbol;

	public uint SubElements;

	public DEBUG_SYMBOL Flags;

	public ulong Reserved;
}
