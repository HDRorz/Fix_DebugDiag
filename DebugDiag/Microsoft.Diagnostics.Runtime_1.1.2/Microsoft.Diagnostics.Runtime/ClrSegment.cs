using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrSegment
{
	public abstract ulong Start { get; }

	public abstract ulong End { get; }

	public ulong Length => End - Start;

	public abstract ClrHeap Heap { get; }

	public abstract int ProcessorAffinity { get; }

	public virtual ulong ReservedEnd => 0uL;

	public virtual ulong CommittedEnd => 0uL;

	public abstract ulong FirstObject { get; }

	public virtual bool IsLarge => false;

	public virtual bool IsEphemeral => false;

	public virtual bool IsReadOnly => false;

	public virtual ulong Gen0Start => Start;

	public virtual ulong Gen0Length => Length;

	public virtual ulong Gen1Start => End;

	public virtual ulong Gen1Length => 0uL;

	public virtual ulong Gen2Start => End;

	public virtual ulong Gen2Length => 0uL;

	public abstract ulong GetFirstObject(out ClrType type);

	public abstract ulong NextObject(ulong objRef);

	public abstract ulong NextObject(ulong objRef, out ClrType type);

	public abstract IEnumerable<ulong> EnumerateObjectAddresses();

	public virtual int GetGeneration(ulong obj)
	{
		if (Gen0Start <= obj && obj < Gen0Start + Gen0Length)
		{
			return 0;
		}
		if (Gen1Start <= obj && obj < Gen1Start + Gen1Length)
		{
			return 1;
		}
		if (Gen2Start <= obj && obj < Gen2Start + Gen2Length)
		{
			return 2;
		}
		return -1;
	}

	public override string ToString()
	{
		return $"HeapSegment {(double)Length / 1000000.0:n2}mb [{Start:X8}, {End:X8}]";
	}
}
