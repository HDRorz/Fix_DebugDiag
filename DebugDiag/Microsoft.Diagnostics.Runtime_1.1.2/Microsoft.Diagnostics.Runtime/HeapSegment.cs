using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal class HeapSegment : ClrSegment
{
	private readonly bool _large;

	private readonly RuntimeBase _clr;

	private readonly ISegmentData _segment;

	private readonly SubHeap _subHeap;

	private readonly HeapBase _heap;

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

	public override bool IsReadOnly => _segment.ReadOnly;

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
			ulong gen2Start = Gen2Start;
			if (gen2Start >= End)
			{
				return 0uL;
			}
			_heap.MemoryReader.EnsureRangeInCache(gen2Start);
			return gen2Start;
		}
	}

	public override bool IsEphemeral => _segment.Address == _subHeap.EphemeralSegment;

	public override IEnumerable<ulong> EnumerateObjectAddresses()
	{
		for (ulong obj = FirstObject; obj != 0L; obj = NextObject(obj))
		{
			yield return obj;
		}
	}

	public override ulong GetFirstObject(out ClrType type)
	{
		ulong gen2Start = Gen2Start;
		if (gen2Start >= End)
		{
			type = null;
			return 0uL;
		}
		_heap.MemoryReader.EnsureRangeInCache(gen2Start);
		type = _heap.GetObjectType(gen2Start);
		return gen2Start;
	}

	public override ulong NextObject(ulong objRef)
	{
		if (objRef >= CommittedEnd)
		{
			return 0uL;
		}
		uint num = (uint)(_clr.PointerSize * 3);
		ClrType objectType = _heap.GetObjectType(objRef);
		if (objectType == null)
		{
			return 0uL;
		}
		ulong size = objectType.GetSize(objRef);
		size = Align(size, _large);
		if (size < num)
		{
			size = num;
		}
		objRef += size;
		if (objRef >= End)
		{
			return 0uL;
		}
		ulong value;
		while (!IsLarge && _subHeap.AllocPointers.TryGetValue(objRef, out value))
		{
			value += Align(num, _large);
			if (objRef >= value)
			{
				return 0uL;
			}
			objRef = value;
			if (objRef >= End)
			{
				return 0uL;
			}
		}
		return objRef;
	}

	public override ulong NextObject(ulong objRef, out ClrType type)
	{
		if (objRef >= CommittedEnd)
		{
			type = null;
			return 0uL;
		}
		uint num = (uint)(_clr.PointerSize * 3);
		ClrType objectType = _heap.GetObjectType(objRef);
		if (objectType == null)
		{
			type = null;
			return 0uL;
		}
		ulong size = objectType.GetSize(objRef);
		size = Align(size, _large);
		if (size < num)
		{
			size = num;
		}
		objRef += size;
		if (objRef >= End)
		{
			type = null;
			return 0uL;
		}
		ulong value;
		while (!IsLarge && _subHeap.AllocPointers.TryGetValue(objRef, out value))
		{
			value += Align(num, _large);
			if (objRef >= value)
			{
				type = null;
				return 0uL;
			}
			objRef = value;
			if (objRef >= End)
			{
				type = null;
				return 0uL;
			}
		}
		type = _heap.GetObjectType(objRef);
		return objRef;
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
