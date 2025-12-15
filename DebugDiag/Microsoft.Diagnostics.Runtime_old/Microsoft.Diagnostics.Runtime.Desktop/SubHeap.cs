using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class SubHeap
{
	internal int HeapNum { get; private set; }

	private IHeapDetails ActualHeap { get; set; }

	internal Dictionary<ulong, ulong> AllocPointers { get; set; }

	internal ulong EphemeralSegment => ActualHeap.EphemeralSegment;

	internal ulong EphemeralEnd => ActualHeap.EphemeralEnd;

	internal ulong Gen0Start => ActualHeap.Gen0Start;

	internal ulong Gen1Start => ActualHeap.Gen1Start;

	internal ulong Gen2Start => ActualHeap.Gen2Start;

	internal ulong FirstLargeSegment => ActualHeap.FirstLargeHeapSegment;

	internal ulong FirstSegment => ActualHeap.FirstHeapSegment;

	internal ulong FQStart => ActualHeap.FQAllObjectsStart;

	internal ulong FQStop => ActualHeap.FQAllObjectsStop;

	internal ulong FQLiveStart => ActualHeap.FQRootsStart;

	internal ulong FQLiveStop => ActualHeap.FQRootsEnd;

	internal SubHeap(IHeapDetails heap, int heapNum)
	{
		ActualHeap = heap;
		HeapNum = heapNum;
	}
}
