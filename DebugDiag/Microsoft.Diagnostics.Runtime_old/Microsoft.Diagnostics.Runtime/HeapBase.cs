using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal abstract class HeapBase : ClrHeap
{
	private ulong _minAddr;

	private ulong _maxAddr;

	private ClrSegment[] _segments;

	private ulong[] _sizeByGen = new ulong[4];

	private ulong _totalHeapSize;

	private int _lastSegmentIdx;

	private bool _canWalkHeap;

	private int _pointerSize;

	internal int Revision { get; set; }

	public override int PointerSize => _pointerSize;

	public override bool CanWalkHeap => _canWalkHeap;

	public override IList<ClrSegment> Segments
	{
		get
		{
			if (Revision != GetRuntimeRevision())
			{
				ClrDiagnosticsException.ThrowRevisionError(Revision, GetRuntimeRevision());
			}
			return _segments;
		}
	}

	public override ulong TotalHeapSize => _totalHeapSize;

	internal MemoryReader MemoryReader { get; private set; }

	public HeapBase(RuntimeBase runtime)
	{
		_canWalkHeap = runtime.CanWalkHeap;
		if (runtime.DataReader.CanReadAsync)
		{
			MemoryReader = new AsyncMemoryReader(runtime.DataReader, 65536);
		}
		else
		{
			MemoryReader = new MemoryReader(runtime.DataReader, 65536);
		}
		_pointerSize = runtime.PointerSize;
	}

	public override bool ReadPointer(ulong addr, out ulong value)
	{
		if (MemoryReader.Contains(addr))
		{
			return MemoryReader.ReadPtr(addr, out value);
		}
		return GetRuntime().ReadPointer(addr, out value);
	}

	protected abstract int GetRuntimeRevision();

	public override ulong GetSizeByGen(int gen)
	{
		return _sizeByGen[gen];
	}

	public override ClrType GetTypeByName(string name)
	{
		foreach (ClrModule item in GetRuntime().EnumerateModules())
		{
			ClrType typeByName = item.GetTypeByName(name);
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
			UpdateSegments(list.ToArray());
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

	public override IEnumerable<ulong> EnumerateObjects()
	{
		if (Revision != GetRuntimeRevision())
		{
			ClrDiagnosticsException.ThrowRevisionError(Revision, GetRuntimeRevision());
		}
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
}
