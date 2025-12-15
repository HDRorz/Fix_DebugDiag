using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrHeap
{
	public virtual bool HasComponentMethodTables => true;

	public abstract ClrRuntime Runtime { get; }

	public abstract IList<ClrSegment> Segments { get; }

	public abstract ClrRootStackwalkPolicy StackwalkPolicy { get; set; }

	public virtual bool IsHeapCached => false;

	public abstract bool AreRootsCached { get; }

	public abstract ClrType Free { get; }

	public abstract bool CanWalkHeap { get; }

	public abstract ulong TotalHeapSize { get; }

	public abstract int PointerSize { get; }

	protected internal virtual long TotalObjects => -1L;

	public abstract ClrType GetObjectType(ulong objRef);

	public ClrObject GetObject(ulong objRef)
	{
		return ClrObject.Create(objRef, GetObjectType(objRef));
	}

	public abstract bool TryGetMethodTable(ulong obj, out ulong methodTable, out ulong componentMethodTable);

	public abstract ulong GetMethodTable(ulong obj);

	public virtual ulong GetEEClassByMethodTable(ulong methodTable)
	{
		return 0uL;
	}

	public virtual ulong GetMethodTableByEEClass(ulong eeclass)
	{
		return 0uL;
	}

	public virtual ClrException GetExceptionObject(ulong objRef)
	{
		return null;
	}

	public abstract IEnumerable<ClrRoot> EnumerateRoots();

	public virtual void CacheHeap(CancellationToken cancelToken)
	{
		throw new NotImplementedException();
	}

	public virtual void ClearHeapCache()
	{
		throw new NotImplementedException();
	}

	public abstract void CacheRoots(CancellationToken cancelToken);

	protected internal virtual void BuildDependentHandleMap(CancellationToken cancelToken)
	{
	}

	protected internal virtual IEnumerable<ClrRoot> EnumerateStackRoots()
	{
		throw new NotImplementedException();
	}

	protected internal virtual IEnumerable<ClrHandle> EnumerateStrongHandles()
	{
		throw new NotImplementedException();
	}

	public abstract void ClearRootCache();

	public abstract ClrType GetTypeByName(string name);

	public abstract ClrType GetTypeByMethodTable(ulong methodTable, ulong componentMethodTable);

	public virtual ClrType GetTypeByMethodTable(ulong methodTable)
	{
		return GetTypeByMethodTable(methodTable, 0uL);
	}

	public abstract IEnumerable<ClrRoot> EnumerateRoots(bool enumerateStatics);

	public virtual IEnumerable<ClrType> EnumerateTypes()
	{
		return null;
	}

	public virtual IEnumerable<ulong> EnumerateFinalizableObjectAddresses()
	{
		throw new NotImplementedException();
	}

	[Obsolete]
	public virtual IEnumerable<BlockingObject> EnumerateBlockingObjects()
	{
		throw new NotImplementedException();
	}

	public abstract IEnumerable<ulong> EnumerateObjectAddresses();

	public abstract IEnumerable<ClrObject> EnumerateObjects();

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

	protected internal abstract IEnumerable<ClrObject> EnumerateObjectReferences(ulong obj, ClrType type, bool carefully);

	protected internal abstract IEnumerable<ClrObjectReference> EnumerateObjectReferencesWithFields(ulong obj, ClrType type, bool carefully);

	protected internal abstract void EnumerateObjectReferences(ulong obj, ClrType type, bool carefully, Action<ulong, int> callback);
}
