namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct JitManagerInfo
{
	public readonly ulong Address;

	public readonly CodeHeapType Type;

	public readonly ulong HeapList;
}
