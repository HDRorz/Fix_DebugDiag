using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class SubHeap
{
	internal int HeapNum { get; }

	private IHeapDetails ActualHeap { get; }

	internal Dictionary<ulong, ulong> AllocPointers { get; set; }

	internal ulong EphemeralSegment => ActualHeap.EphemeralSegment;

	internal ulong EphemeralEnd => ActualHeap.EphemeralEnd;

	internal ulong Gen0Start => ActualHeap.Gen0Start;

	internal ulong Gen1Start => ActualHeap.Gen1Start;

	internal ulong Gen2Start => ActualHeap.Gen2Start;

	internal ulong FirstLargeSegment => ActualHeap.FirstLargeHeapSegment;

	internal ulong FirstSegment => ActualHeap.FirstHeapSegment;

	internal ulong FQAllObjectsStart => ActualHeap.FQAllObjectsStart;

	internal ulong FQAllObjectsStop => ActualHeap.FQAllObjectsStop;

	internal ulong FQRootsStart => ActualHeap.FQRootsStart;

	internal ulong FQRootsStop => ActualHeap.FQRootsStop;

	internal SubHeap(IHeapDetails heap, int heapNum, Dictionary<ulong, ulong> allocPointers)
	{
		ActualHeap = heap;
		HeapNum = heapNum;
		AllocPointers = allocPointers;
	}
}
