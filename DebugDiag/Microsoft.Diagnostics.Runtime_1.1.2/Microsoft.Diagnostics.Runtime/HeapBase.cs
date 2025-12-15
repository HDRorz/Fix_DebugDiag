using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal abstract class HeapBase : ClrHeap
{
	protected static readonly ClrObject[] s_emptyObjectSet = new ClrObject[0];

	protected static readonly ClrObjectReference[] s_emptyObjectReferenceSet = new ClrObjectReference[0];

	private ulong _minAddr;

	private ulong _maxAddr;

	private ClrSegment[] _segments;

	private ulong[] _sizeByGen = new ulong[4];

	private ulong _totalHeapSize;

	private int _lastSegmentIdx;

	internal int Revision { get; set; }

	public override int PointerSize { get; }

	public override bool CanWalkHeap { get; }

	public override IList<ClrSegment> Segments
	{
		get
		{
			RevisionValidator.Validate(Revision, GetRuntimeRevision());
			return _segments;
		}
	}

	public override ulong TotalHeapSize => _totalHeapSize;

	internal MemoryReader MemoryReader { get; }

	public HeapBase(RuntimeBase runtime)
	{
		CanWalkHeap = runtime.CanWalkHeap;
		MemoryReader = new MemoryReader(runtime.DataReader, 65536);
		PointerSize = runtime.PointerSize;
	}

	public override ulong GetMethodTable(ulong obj)
	{
		if (MemoryReader.ReadPtr(obj, out var value))
		{
			return value;
		}
		return 0uL;
	}

	public override bool ReadPointer(ulong addr, out ulong value)
	{
		if (MemoryReader.Contains(addr))
		{
			return MemoryReader.ReadPtr(addr, out value);
		}
		return Runtime.ReadPointer(addr, out value);
	}

	protected abstract int GetRuntimeRevision();

	public override ulong GetSizeByGen(int gen)
	{
		return _sizeByGen[gen];
	}

	public override ClrType GetTypeByName(string name)
	{
		foreach (ClrModule module in Runtime.Modules)
		{
			ClrType typeByName = module.GetTypeByName(name);
			if (typeByName != null)
			{
				return typeByName;
			}
		}
		return null;
	}

	protected void UpdateSegmentData(HeapSegment segment)
	{
		_totalHeapSize += segment.Length;
		_sizeByGen[0] += segment.Gen0Length;
		_sizeByGen[1] += segment.Gen1Length;
		if (!segment.IsLarge)
		{
			_sizeByGen[2] += segment.Gen2Length;
		}
		else
		{
			_sizeByGen[3] += segment.Gen2Length;
		}
	}

	protected void InitSegments(RuntimeBase runtime)
	{
		if (runtime.GetHeaps(out var heaps))
		{
			List<HeapSegment> list = new List<HeapSegment>();
			SubHeap[] array = heaps;
			foreach (SubHeap subHeap in array)
			{
				if (subHeap != null)
				{
					for (ISegmentData segmentData = runtime.GetSegmentData(subHeap.FirstLargeSegment); segmentData != null; segmentData = runtime.GetSegmentData(segmentData.Next))
					{
						HeapSegment heapSegment = new HeapSegment(runtime, segmentData, subHeap, large: true, this);
						list.Add(heapSegment);
						UpdateSegmentData(heapSegment);
					}
					for (ISegmentData segmentData = runtime.GetSegmentData(subHeap.FirstSegment); segmentData != null; segmentData = runtime.GetSegmentData(segmentData.Next))
					{
						HeapSegment heapSegment2 = new HeapSegment(runtime, segmentData, subHeap, large: false, this);
						list.Add(heapSegment2);
						UpdateSegmentData(heapSegment2);
					}
				}
			}
			ClrSegment[] segments = list.ToArray();
			UpdateSegments(segments);
		}
		else
		{
			_segments = new ClrSegment[0];
		}
	}

	private void UpdateSegments(ClrSegment[] segments)
	{
		Array.Sort(segments, (ClrSegment x, ClrSegment y) => x.Start.CompareTo(y.Start));
		_segments = segments;
		_minAddr = ulong.MaxValue;
		_maxAddr = 0uL;
		_totalHeapSize = 0uL;
		_sizeByGen = new ulong[4];
		ClrSegment[] segments2 = _segments;
		foreach (ClrSegment clrSegment in segments2)
		{
			if (clrSegment.Start < _minAddr)
			{
				_minAddr = clrSegment.Start;
			}
			if (_maxAddr < clrSegment.End)
			{
				_maxAddr = clrSegment.End;
			}
			_totalHeapSize += clrSegment.Length;
			if (clrSegment.IsLarge)
			{
				_sizeByGen[3] += clrSegment.Length;
				continue;
			}
			_sizeByGen[2] += clrSegment.Gen2Length;
			_sizeByGen[1] += clrSegment.Gen1Length;
			_sizeByGen[0] += clrSegment.Gen0Length;
		}
	}

	public override IEnumerable<ClrObject> EnumerateObjects()
	{
		RevisionValidator.Validate(Revision, GetRuntimeRevision());
		int i = 0;
		while (i < _segments.Length)
		{
			ClrSegment seg = _segments[i];
			ClrType type;
			for (ulong obj = seg.GetFirstObject(out type); obj != 0L; obj = seg.NextObject(obj, out type))
			{
				_lastSegmentIdx = i;
				yield return ClrObject.Create(obj, type);
			}
			int num = i + 1;
			i = num;
		}
	}

	public override IEnumerable<ulong> EnumerateObjectAddresses()
	{
		RevisionValidator.Validate(Revision, GetRuntimeRevision());
		int i = 0;
		while (i < _segments.Length)
		{
			ClrSegment seg = _segments[i];
			for (ulong obj = seg.FirstObject; obj != 0L; obj = seg.NextObject(obj))
			{
				_lastSegmentIdx = i;
				yield return obj;
			}
			int num = i + 1;
			i = num;
		}
	}

	public override ClrSegment GetSegmentByAddress(ulong objRef)
	{
		if (_minAddr <= objRef && objRef < _maxAddr)
		{
			int num = _lastSegmentIdx;
			do
			{
				ClrSegment clrSegment = _segments[num];
				long num2 = (long)(objRef - clrSegment.Start);
				if (0 <= num2 && num2 < (long)clrSegment.Length)
				{
					_lastSegmentIdx = num;
					return clrSegment;
				}
				num++;
				if (num >= Segments.Count)
				{
					num = 0;
				}
			}
			while (num != _lastSegmentIdx);
		}
		return null;
	}

	protected internal override IEnumerable<ClrObject> EnumerateObjectReferences(ulong obj, ClrType type, bool carefully)
	{
		if (type == null)
		{
			type = GetObjectType(obj);
		}
		if (type == null || (!type.ContainsPointers && !type.IsCollectible))
		{
			return s_emptyObjectSet;
		}
		List<ClrObject> result = null;
		if (type.ContainsPointers)
		{
			GCDesc gCDesc = type.GCDesc;
			if (gCDesc == null)
			{
				return s_emptyObjectSet;
			}
			ulong size = type.GetSize(obj);
			if (carefully)
			{
				ClrSegment segmentByAddress = GetSegmentByAddress(obj);
				if (segmentByAddress == null || obj + size > segmentByAddress.End || (!segmentByAddress.IsLarge && size > 85000))
				{
					return s_emptyObjectSet;
				}
			}
			result = new List<ClrObject>();
			MemoryReader reader = GetMemoryReaderForAddress(obj);
			gCDesc.WalkObject(obj, size, (ulong ptr) => ReadPointer(reader, ptr), delegate(ulong reference, int offset)
			{
				result.Add(new ClrObject(reference, GetObjectType(reference)));
			});
		}
		if (type.IsCollectible)
		{
			result = result ?? new List<ClrObject>(1);
			result.Add(GetObject(type.LoaderAllocatorObject));
		}
		return result;
	}

	protected internal override IEnumerable<ClrObjectReference> EnumerateObjectReferencesWithFields(ulong obj, ClrType type, bool carefully)
	{
		if (type == null)
		{
			type = GetObjectType(obj);
		}
		if (type == null || (!type.ContainsPointers && !type.IsCollectible))
		{
			return s_emptyObjectReferenceSet;
		}
		List<ClrObjectReference> result = null;
		if (type.ContainsPointers)
		{
			GCDesc gCDesc = type.GCDesc;
			if (gCDesc == null)
			{
				return s_emptyObjectReferenceSet;
			}
			ulong size = type.GetSize(obj);
			if (carefully)
			{
				ClrSegment segmentByAddress = GetSegmentByAddress(obj);
				if (segmentByAddress == null || obj + size > segmentByAddress.End || (!segmentByAddress.IsLarge && size > 85000))
				{
					return s_emptyObjectReferenceSet;
				}
			}
			result = new List<ClrObjectReference>();
			MemoryReader reader = GetMemoryReaderForAddress(obj);
			gCDesc.WalkObject(obj, size, (ulong ptr) => ReadPointer(reader, ptr), delegate(ulong reference, int offset)
			{
				result.Add(new ClrObjectReference(offset, reference, GetObjectType(reference)));
			});
		}
		if (type.IsCollectible)
		{
			result = result ?? new List<ClrObjectReference>(1);
			ulong loaderAllocatorObject = type.LoaderAllocatorObject;
			result.Add(new ClrObjectReference(-1, loaderAllocatorObject, GetObjectType(loaderAllocatorObject)));
		}
		return result;
	}

	protected internal override void EnumerateObjectReferences(ulong obj, ClrType type, bool carefully, Action<ulong, int> callback)
	{
		if (type == null)
		{
			type = GetObjectType(obj);
		}
		if (type.ContainsPointers)
		{
			GCDesc gCDesc = type.GCDesc;
			if (gCDesc == null)
			{
				return;
			}
			ulong size = type.GetSize(obj);
			if (carefully)
			{
				ClrSegment segmentByAddress = GetSegmentByAddress(obj);
				if (segmentByAddress == null || obj + size > segmentByAddress.End || (!segmentByAddress.IsLarge && size > 85000))
				{
					return;
				}
			}
			MemoryReader reader = GetMemoryReaderForAddress(obj);
			gCDesc.WalkObject(obj, size, (ulong ptr) => ReadPointer(reader, ptr), callback);
		}
		if (type.IsCollectible)
		{
			callback(type.LoaderAllocatorObject, -1);
		}
	}

	private ulong ReadPointer(MemoryReader reader, ulong addr)
	{
		if (reader.ReadPtr(addr, out var value))
		{
			return value;
		}
		return 0uL;
	}

	protected abstract MemoryReader GetMemoryReaderForAddress(ulong obj);
}
