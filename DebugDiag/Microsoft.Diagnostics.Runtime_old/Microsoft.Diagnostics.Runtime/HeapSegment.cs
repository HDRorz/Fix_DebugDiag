using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal class HeapSegment : ClrSegment
{
	private bool _large;

	private RuntimeBase _clr;

	private ISegmentData _segment;

	private SubHeap _subHeap;

	private HeapBase _heap;

	public override int ProcessorAffinity => _subHeap.HeapNum;

	public override ulong Start => _segment.Start;

	public override ulong End
	{
		get
		{
			if (_subHeap.EphemeralSegment != _segment.Address)
			{
				return _segment.End;
			}
			return _subHeap.EphemeralEnd;
		}
	}

	public override ClrHeap Heap => _heap;

	public override bool IsLarge => _large;

	public override ulong ReservedEnd => _segment.Reserved;

	public override ulong CommittedEnd => _segment.Committed;

	public override ulong Gen0Start
	{
		get
		{
			if (IsEphemeral)
			{
				return _subHeap.Gen0Start;
			}
			return End;
		}
	}

	public override ulong Gen0Length => End - Gen0Start;

	public override ulong Gen1Start
	{
		get
		{
			if (IsEphemeral)
			{
				return _subHeap.Gen1Start;
			}
			return End;
		}
	}

	public override ulong Gen1Length => Gen0Start - Gen1Start;

	public override ulong Gen2Start => Start;

	public override ulong Gen2Length => Gen1Start - Start;

	public override ulong FirstObject
	{
		get
		{
			if (Gen2Start == End)
			{
				return 0uL;
			}
			_heap.MemoryReader.EnsureRangeInCache(Gen2Start);
			return Gen2Start;
		}
	}

	public override bool IsEphemeral => _segment.Address == _subHeap.EphemeralSegment;

	public override IEnumerable<ulong> EnumerateObjects()
	{
		for (ulong obj = FirstObject; obj != 0L; obj = NextObject(obj))
		{
			yield return obj;
		}
	}

	public override ulong NextObject(ulong addr)
	{
		if (addr >= CommittedEnd)
		{
			return 0uL;
		}
		uint num = (uint)(_clr.PointerSize * 3);
		ClrType objectType = _heap.GetObjectType(addr);
		if (objectType == null)
		{
			return 0uL;
		}
		ulong size = objectType.GetSize(addr);
		size = Align(size, _large);
		if (size < num)
		{
			size = num;
		}
		addr += size;
		if (addr >= End)
		{
			return 0uL;
		}
		ulong value;
		while (!IsLarge && _subHeap.AllocPointers.TryGetValue(addr, out value))
		{
			value += Align(num, _large);
			if (addr >= value)
			{
				return 0uL;
			}
			addr = value;
			if (addr >= End)
			{
				return 0uL;
			}
		}
		return addr;
	}

	internal static ulong Align(ulong size, bool large)
	{
		ulong num = 7uL;
		ulong num2 = (ulong)((IntPtr.Size != 4) ? 7 : 3);
		if (large)
		{
			return (size + num) & ~num;
		}
		return (size + num2) & ~num2;
	}

	internal HeapSegment(RuntimeBase clr, ISegmentData segment, SubHeap subHeap, bool large, HeapBase heap)
	{
		_clr = clr;
		_large = large;
		_segment = segment;
		_heap = heap;
		_subHeap = subHeap;
	}
}
