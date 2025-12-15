using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeType : ClrType
{
	private string _name;

	private ulong _eeType;

	private NativeHeap _heap;

	private NativeModule _module;

	private uint _baseSize;

	private uint _componentSize;

	private GCDesc _gcDesc;

	private bool _containsPointers;

	private int _index;

	public override int Index => _index;

	public override ClrModule Module => _module;

	public override string Name => _name;

	public override ClrHeap Heap
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override IList<ClrInterface> Interfaces
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsFinalizable
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsPublic
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsPrivate
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsInternal
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsProtected
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsAbstract
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsSealed
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsInterface
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override ClrType BaseType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override int ElementSize
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override int BaseSize
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override uint MetadataToken
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public NativeType(NativeHeap heap, int index, NativeModule module, string name, ulong eeType, IMethodTableData mtData)
	{
		_heap = heap;
		_module = module;
		_name = name;
		_eeType = eeType;
		_index = index;
		_baseSize = mtData.BaseSize;
		_componentSize = mtData.ComponentSize;
		_containsPointers = mtData.ContainsPointers;
	}

	public override ulong GetSize(ulong objRef)
	{
		uint pointerSize = (uint)_heap.PointerSize;
		ulong num;
		if (_componentSize == 0)
		{
			num = _baseSize;
		}
		else
		{
			uint value = 0u;
			uint num2 = pointerSize;
			ulong addr = objRef + num2;
			MemoryReader memoryReader = _heap.MemoryReader;
			if (!memoryReader.Contains(addr))
			{
				memoryReader = _heap.NativeRuntime.MemoryReader;
			}
			if (!memoryReader.ReadDword(addr, out value))
			{
				throw new Exception("Could not read from heap at " + objRef.ToString("x"));
			}
			num = (ulong)((long)value * (long)_componentSize + _baseSize);
		}
		uint num3 = pointerSize * 3;
		if (num < num3)
		{
			num = num3;
		}
		return num;
	}

	public override void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> action)
	{
		if (_containsPointers && (_gcDesc != null || (FillGCDesc() && _gcDesc != null)))
		{
			ulong size = GetSize(objRef);
			MemoryReader memoryReader = _heap.MemoryReader;
			if (!memoryReader.Contains(objRef))
			{
				memoryReader = _heap.NativeRuntime.MemoryReader;
			}
			_gcDesc.WalkObject(objRef, size, memoryReader, action);
		}
	}

	private bool FillGCDesc()
	{
		NativeRuntime nativeRuntime = _heap.NativeRuntime;
		if (!nativeRuntime.MemoryReader.TryReadDword(_eeType - (ulong)IntPtr.Size, out int value))
		{
			return false;
		}
		if (value < 0)
		{
			value = -value;
		}
		int num = 1 + value * 2;
		byte[] array = new byte[num * IntPtr.Size];
		if (!nativeRuntime.ReadMemory(_eeType - (ulong)(num * IntPtr.Size), array, array.Length, out var bytesRead) || bytesRead != array.Length)
		{
			return false;
		}
		_gcDesc = new GCDesc(array);
		return true;
	}

	public override bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset)
	{
		throw new NotImplementedException();
	}

	public override ClrInstanceField GetFieldByName(string name)
	{
		throw new NotImplementedException();
	}

	public override int GetArrayLength(ulong objRef)
	{
		throw new NotImplementedException();
	}

	public override ulong GetArrayElementAddress(ulong objRef, int index)
	{
		throw new NotImplementedException();
	}

	public override object GetArrayElementValue(ulong objRef, int index)
	{
		throw new NotImplementedException();
	}

	public override ClrStaticField GetStaticFieldByName(string name)
	{
		throw new NotImplementedException();
	}

	public override void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action)
	{
		EnumerateRefsOfObject(objRef, action);
	}
}
