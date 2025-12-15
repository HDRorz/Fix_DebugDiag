namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyJitManagerInfo
{
	public ulong addr;

	public CodeHeapType type;

	public ulong ptrHeapList;
}
