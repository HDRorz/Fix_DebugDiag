using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrHeap
{
	public abstract IList<ClrSegment> Segments { get; }

	public abstract int TypeIndexLimit { get; }

	public abstract bool CanWalkHeap { get; }

	public abstract ulong TotalHeapSize { get; }

	public abstract int PointerSize { get; }

	public abstract ClrType GetObjectType(ulong objRef);

	public virtual ClrException GetExceptionObject(ulong objRef)
	{
		return null;
	}

	public virtual ClrRuntime GetRuntime()
	{
		return null;
	}

	public abstract IEnumerable<ClrRoot> EnumerateRoots();

	public abstract ClrType GetTypeByIndex(int index);

	public abstract ClrType GetTypeByName(string name);

	public abstract IEnumerable<ClrRoot> EnumerateRoots(bool enumerateStatics);

	public virtual IEnumerable<ClrType> EnumerateTypes()
	{
		return null;
	}

	public virtual IEnumerable<ulong> EnumerateFinalizableObjects()
	{
		throw new NotImplementedException();
	}

	public virtual IEnumerable<BlockingObject> EnumerateBlockingObjects()
	{
		throw new NotImplementedException();
	}

	public abstract IEnumerable<ulong> EnumerateObjects();

	public abstract ulong GetSizeByGen(int gen);

	public int GetGeneration(ulong obj)
	{
		return GetSegmentByAddress(obj)?.GetGeneration(obj) ?? (-1);
	}

	public virtual ulong NextObject(ulong obj)
	{
		return GetSegmentByAddress(obj)?.NextObject(obj) ?? 0;
	}

	public abstract ClrSegment GetSegmentByAddress(ulong objRef);

	public bool IsInHeap(ulong address)
	{
		return GetSegmentByAddress(address) != null;
	}

	public override string ToString()
	{
		double num = (double)TotalHeapSize / 1000000.0;
		int num2 = ((Segments != null) ? Segments.Count : 0);
		return $"ClrHeap {num}mb {num2} segments";
	}

	public virtual int ReadMemory(ulong address, byte[] buffer, int offset, int count)
	{
		return 0;
	}

	public abstract bool ReadPointer(ulong addr, out ulong value);
}
