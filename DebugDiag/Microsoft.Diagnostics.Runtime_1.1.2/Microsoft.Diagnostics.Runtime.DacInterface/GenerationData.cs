using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct GenerationData
{
	public readonly ulong StartSegment;

	public readonly ulong AllocationStart;

	public readonly ulong AllocationContextPointer;

	public readonly ulong AllocationContextLimit;

	internal GenerationData(ref GenerationData other)
	{
		this = other;
		if (IntPtr.Size == 4)
		{
			FixupPointer(ref StartSegment);
			FixupPointer(ref AllocationStart);
			FixupPointer(ref AllocationContextPointer);
			FixupPointer(ref AllocationContextLimit);
		}
	}

	private static void FixupPointer(ref ulong ptr)
	{
		ptr = (uint)ptr;
	}
}
