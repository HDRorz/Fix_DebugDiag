using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeSegementData : ISegmentData
{
	public ulong segmentAddr;

	public ulong allocated;

	public ulong committed;

	public ulong reserved;

	public ulong used;

	public ulong mem;

	public ulong next;

	public ulong gc_heap;

	public ulong highAllocMark;

	public uint isReadOnly;

	public ulong Address => segmentAddr;

	public ulong Next => next;

	public ulong Start => mem;

	public ulong End => allocated;

	public ulong Reserved => reserved;

	public ulong Committed => committed;
}
