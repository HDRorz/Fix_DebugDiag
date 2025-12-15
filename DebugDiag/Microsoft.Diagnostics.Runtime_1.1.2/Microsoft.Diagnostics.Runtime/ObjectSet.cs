using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public class ObjectSet
{
	protected struct HeapHashSegment
	{
		public BitArray Objects;

		public ulong StartAddress;

		public ulong EndAddress;
	}

	protected ClrHeap _heap;

	protected readonly int _minObjSize;

	protected HeapHashSegment[] _segments;

	public int Count { get; protected set; }

	public ObjectSet(ClrHeap heap)
	{
		_heap = heap;
		_minObjSize = heap.PointerSize * 3;
		List<HeapHashSegment> list = new List<HeapHashSegment>(_heap.Segments.Count);
		foreach (ClrSegment segment in _heap.Segments)
		{
			ulong start = segment.Start;
			ulong end = segment.End;
			if (start < end)
			{
				list.Add(new HeapHashSegment
				{
					StartAddress = start,
					EndAddress = end,
					Objects = new BitArray((int)(end - start) / _minObjSize, defaultValue: false)
				});
			}
		}
		_segments = list.ToArray();
	}

	public virtual bool Contains(ulong obj)
	{
		if (GetSegment(obj, out var seg))
		{
			int offset = GetOffset(obj, seg);
			return seg.Objects[offset];
		}
		return false;
	}

	public virtual bool Add(ulong obj)
	{
		if (GetSegment(obj, out var seg))
		{
			int offset = GetOffset(obj, seg);
			if (seg.Objects[offset])
			{
				return false;
			}
			seg.Objects.Set(offset, value: true);
			Count++;
			return true;
		}
		return false;
	}

	public virtual bool Remove(ulong obj)
	{
		if (GetSegment(obj, out var seg))
		{
			int offset = GetOffset(obj, seg);
			if (seg.Objects[offset])
			{
				seg.Objects.Set(offset, value: false);
				Count--;
				return true;
			}
		}
		return false;
	}

	public virtual void Clear()
	{
		for (int i = 0; i < _segments.Length; i++)
		{
			_segments[i].Objects.SetAll(value: false);
		}
		Count = 0;
	}

	protected int GetOffset(ulong obj, HeapHashSegment seg)
	{
		return checked((int)(obj - seg.StartAddress)) / _minObjSize;
	}

	protected bool GetSegment(ulong obj, out HeapHashSegment seg)
	{
		if (obj != 0L)
		{
			int num = 0;
			int num2 = _segments.Length - 1;
			while (num <= num2)
			{
				int num3 = num + num2 >> 1;
				if (obj < _segments[num3].StartAddress)
				{
					num2 = num3 - 1;
					continue;
				}
				if (obj >= _segments[num3].EndAddress)
				{
					num = num3 + 1;
					continue;
				}
				seg = _segments[num3];
				return true;
			}
		}
		seg = default(HeapHashSegment);
		return false;
	}
}
