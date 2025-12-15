using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal abstract class RuntimeBase : ClrRuntime
{
	private struct StackRef
	{
		public ulong Address;

		public ulong Object;

		public StackRef(ulong stackPtr, ulong objRef)
		{
			Address = stackPtr;
			Object = objRef;
		}
	}

	private static ulong[] s_emptyPointerArray = new ulong[0];

	protected DacLibrary _library;

	protected IXCLRDataProcess _dacInterface;

	private MemoryReader _cache;

	protected IDataReader _dataReader;

	protected DataTargetImpl _dataTarget;

	private byte[] _dataBuffer = new byte[8];

	public override DataTarget DataTarget => _dataTarget;

	public IDataReader DataReader => _dataReader;

	public override int PointerSize => IntPtr.Size;

	internal bool CanWalkHeap { get; private set; }

	internal MemoryReader MemoryReader
	{
		get
		{
			if (_cache == null)
			{
				_cache = new MemoryReader(DataReader, 512);
			}
			return _cache;
		}
		set
		{
			_cache = value;
		}
	}

	public RuntimeBase(DataTargetImpl dataTarget, DacLibrary lib)
	{
		_dataTarget = dataTarget;
		_library = lib;
		_dacInterface = _library.DacInterface;
		InitApi();
		_dacInterface.Flush();
		IGCInfo gCInfo = GetGCInfo();
		if (gCInfo == null)
		{
			throw new ClrDiagnosticsException("This runtime is not initialized and contains no data.", ClrDiagnosticsException.HR.RuntimeUninitialized);
		}
		base.ServerGC = gCInfo.ServerMode;
		base.HeapCount = gCInfo.HeapCount;
		CanWalkHeap = gCInfo.GCStructuresValid && !dataTarget.DataReader.IsMinidump;
		_dataReader = dataTarget.DataReader;
	}

	protected abstract void InitApi();

	internal bool GetHeaps(out SubHeap[] heaps)
	{
		heaps = new SubHeap[base.HeapCount];
		Dictionary<ulong, ulong> allocContexts = GetAllocContexts();
		if (base.ServerGC)
		{
			ulong[] serverHeapList = GetServerHeapList();
			if (serverHeapList == null)
			{
				return false;
			}
			bool result = false;
			for (int i = 0; i < serverHeapList.Length; i++)
			{
				IHeapDetails svrHeapDetails = GetSvrHeapDetails(serverHeapList[i]);
				if (svrHeapDetails != null)
				{
					heaps[i] = new SubHeap(svrHeapDetails, i);
					heaps[i].AllocPointers = new Dictionary<ulong, ulong>(allocContexts);
					if (svrHeapDetails.EphemeralAllocContextPtr != 0L)
					{
						heaps[i].AllocPointers[svrHeapDetails.EphemeralAllocContextPtr] = svrHeapDetails.EphemeralAllocContextLimit;
					}
					result = true;
				}
			}
			return result;
		}
		IHeapDetails wksHeapDetails = GetWksHeapDetails();
		if (wksHeapDetails == null)
		{
			return false;
		}
		heaps[0] = new SubHeap(wksHeapDetails, 0);
		heaps[0].AllocPointers = allocContexts;
		heaps[0].AllocPointers[wksHeapDetails.EphemeralAllocContextPtr] = wksHeapDetails.EphemeralAllocContextLimit;
		return true;
	}

	internal Dictionary<ulong, ulong> GetAllocContexts()
	{
		Dictionary<ulong, ulong> dictionary = new Dictionary<ulong, ulong>();
		int num = 1024;
		IThreadData thread = GetThread(GetFirstThread());
		while (num-- > 0 && thread != null)
		{
			if (thread.AllocPtr != 0L)
			{
				dictionary[thread.AllocPtr] = thread.AllocLimit;
			}
			if (thread.Next == 0L)
			{
				break;
			}
			thread = GetThread(thread.Next);
		}
		return dictionary;
	}

	public override IEnumerable<ulong> EnumerateFinalizerQueue()
	{
		if (!GetHeaps(out var heaps))
		{
			yield break;
		}
		SubHeap[] array = heaps;
		foreach (SubHeap subHeap in array)
		{
			foreach (ulong item in GetPointersInRange(subHeap.FQStart, subHeap.FQStop))
			{
				if (item != 0L)
				{
					yield return item;
				}
			}
		}
	}

	internal virtual IEnumerable<ClrRoot> EnumerateStackReferences(ClrThread thread, bool includeDead)
	{
		ulong num = thread.StackBase;
		ulong stackLimit = thread.StackLimit;
		if (stackLimit <= num)
		{
			ulong num2 = stackLimit;
			stackLimit = num;
			num = num2;
		}
		ClrAppDomain domain = GetAppDomainByAddress(thread.AppDomain);
		ClrHeap heap = GetHeap();
		_ = PointerSize;
		MemoryReader cache = MemoryReader;
		cache.EnsureRangeInCache(num);
		for (ulong stackPtr = num; stackPtr < stackLimit; stackPtr += (uint)PointerSize)
		{
			if (cache.ReadPtr(stackPtr, out var value) && heap.IsInHeap(value) && heap.ReadPointer(value, out var value2))
			{
				ClrType clrType = null;
				if (value2 > 1024)
				{
					clrType = heap.GetObjectType(value);
				}
				if (clrType != null && !clrType.IsFree)
				{
					yield return new LocalVarRoot(stackPtr, value, clrType, domain, thread, pinned: false, falsePos: true, interior: false);
				}
			}
		}
	}

	private bool IsInSegment(ClrSegment seg, ulong p)
	{
		if (seg.Start <= p)
		{
			return p <= seg.End;
		}
		return false;
	}

	internal abstract IEnumerable<ClrStackFrame> EnumerateStackFrames(uint osThreadId);

	internal abstract ulong GetFirstThread();

	internal abstract IThreadData GetThread(ulong addr);

	internal abstract IHeapDetails GetSvrHeapDetails(ulong addr);

	internal abstract IHeapDetails GetWksHeapDetails();

	internal abstract ulong[] GetServerHeapList();

	internal abstract IThreadStoreData GetThreadStoreData();

	internal abstract ISegmentData GetSegmentData(ulong addr);

	internal abstract IGCInfo GetGCInfo();

	internal abstract IMethodTableData GetMethodTableData(ulong addr);

	internal abstract uint GetTlsSlot();

	internal abstract uint GetThreadTypeIndex();

	internal abstract ClrAppDomain GetAppDomainByAddress(ulong addr);

	protected bool Request(uint id, ulong param, byte[] output)
	{
		byte[] bytes = BitConverter.GetBytes(param);
		return Request(id, bytes, output);
	}

	protected bool Request(uint id, uint param, byte[] output)
	{
		byte[] bytes = BitConverter.GetBytes(param);
		return Request(id, bytes, output);
	}

	protected bool Request(uint id, byte[] input, byte[] output)
	{
		uint inBufferSize = 0u;
		if (input != null)
		{
			inBufferSize = (uint)input.Length;
		}
		uint outBufferSize = 0u;
		if (output != null)
		{
			outBufferSize = (uint)output.Length;
		}
		return _dacInterface.Request(id, inBufferSize, input, outBufferSize, output) >= 0;
	}

	protected I Request<I, T>(uint id, byte[] input) where I : class where T : struct, I
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		if (!Request(id, input, byteArrayForStruct))
		{
			return null;
		}
		return ConvertStruct<I, T>(byteArrayForStruct);
	}

	protected I Request<I, T>(uint id, ulong param) where I : class where T : struct, I
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		if (!Request(id, param, byteArrayForStruct))
		{
			return null;
		}
		return ConvertStruct<I, T>(byteArrayForStruct);
	}

	protected I Request<I, T>(uint id, uint param) where I : class where T : struct, I
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		if (!Request(id, param, byteArrayForStruct))
		{
			return null;
		}
		return ConvertStruct<I, T>(byteArrayForStruct);
	}

	protected I Request<I, T>(uint id) where I : class where T : struct, I
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		if (!Request(id, null, byteArrayForStruct))
		{
			return null;
		}
		return ConvertStruct<I, T>(byteArrayForStruct);
	}

	protected bool RequestStruct<T>(uint id, ref T t) where T : struct
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		if (!Request(id, null, byteArrayForStruct))
		{
			return false;
		}
		GCHandle gCHandle = GCHandle.Alloc(byteArrayForStruct, GCHandleType.Pinned);
		t = (T)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(T));
		gCHandle.Free();
		return true;
	}

	protected bool RequestStruct<T>(uint id, ulong addr, ref T t) where T : struct
	{
		byte[] array = new byte[8];
		byte[] byteArrayForStruct = GetByteArrayForStruct<T>();
		WriteValueToBuffer(addr, array, 0);
		if (!Request(id, array, byteArrayForStruct))
		{
			return false;
		}
		GCHandle gCHandle = GCHandle.Alloc(byteArrayForStruct, GCHandleType.Pinned);
		t = (T)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(T));
		gCHandle.Free();
		return true;
	}

	protected ulong[] RequestAddrList(uint id, int length)
	{
		byte[] array = new byte[length * 8];
		if (!Request(id, null, array))
		{
			return null;
		}
		ulong[] array2 = new ulong[length];
		for (uint num = 0u; num < length; num++)
		{
			array2[num] = BitConverter.ToUInt64(array, (int)(num * 8));
		}
		return array2;
	}

	protected ulong[] RequestAddrList(uint id, ulong param, int length)
	{
		byte[] array = new byte[length * 8];
		if (!Request(id, param, array))
		{
			return null;
		}
		ulong[] array2 = new ulong[length];
		for (uint num = 0u; num < length; num++)
		{
			array2[num] = BitConverter.ToUInt64(array, (int)(num * 8));
		}
		return array2;
	}

	protected static string BytesToString(byte[] output)
	{
		int i;
		for (i = 0; i < output.Length && (output[i] != 0 || output[i + 1] != 0); i += 2)
		{
		}
		if (i > output.Length)
		{
			i = output.Length;
		}
		return Encoding.Unicode.GetString(output, 0, i);
	}

	protected byte[] GetByteArrayForStruct<T>() where T : struct
	{
		return new byte[Marshal.SizeOf(typeof(T))];
	}

	protected I ConvertStruct<I, T>(byte[] bytes) where I : class where T : I
	{
		GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		I result = (I)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(T));
		gCHandle.Free();
		return result;
	}

	protected int WriteValueToBuffer(IntPtr ptr, byte[] buffer, int offset)
	{
		ulong num = (ulong)ptr.ToInt64();
		for (int i = offset; i < offset + IntPtr.Size; i++)
		{
			buffer[i] = (byte)num;
			num >>= 8;
		}
		return offset + IntPtr.Size;
	}

	protected int WriteValueToBuffer(int value, byte[] buffer, int offset)
	{
		for (int i = offset; i < offset + 4; i++)
		{
			buffer[i] = (byte)value;
			value >>= 8;
		}
		return offset + 4;
	}

	protected int WriteValueToBuffer(uint value, byte[] buffer, int offset)
	{
		for (int i = offset; i < offset + 4; i++)
		{
			buffer[i] = (byte)value;
			value >>= 8;
		}
		return offset + 4;
	}

	protected int WriteValueToBuffer(ulong value, byte[] buffer, int offset)
	{
		for (int i = offset; i < offset + 8; i++)
		{
			buffer[i] = (byte)value;
			value >>= 8;
		}
		return offset + 8;
	}

	public override bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		return _dataReader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	[Obsolete]
	public override bool ReadVirtual(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		return _dataReader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	public bool ReadByte(ulong addr, out byte value)
	{
		value = 0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 1, out bytesRead))
		{
			return false;
		}
		value = _dataBuffer[0];
		return true;
	}

	public bool ReadByte(ulong addr, out sbyte value)
	{
		value = 0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 1, out bytesRead))
		{
			return false;
		}
		value = (sbyte)_dataBuffer[0];
		return true;
	}

	public bool ReadDword(ulong addr, out int value)
	{
		value = 0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 4, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToInt32(_dataBuffer, 0);
		return true;
	}

	public bool ReadDword(ulong addr, out uint value)
	{
		value = 0u;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 4, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToUInt32(_dataBuffer, 0);
		return true;
	}

	public bool ReadFloat(ulong addr, out float value)
	{
		value = 0f;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 4, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToSingle(_dataBuffer, 0);
		return true;
	}

	public bool ReadFloat(ulong addr, out double value)
	{
		value = 0.0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 8, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToDouble(_dataBuffer, 0);
		return true;
	}

	public bool ReadShort(ulong addr, out short value)
	{
		value = 0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 2, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToInt16(_dataBuffer, 0);
		return true;
	}

	public bool ReadShort(ulong addr, out ushort value)
	{
		value = 0;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 2, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToUInt16(_dataBuffer, 0);
		return true;
	}

	public bool ReadQword(ulong addr, out ulong value)
	{
		value = 0uL;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 8, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToUInt64(_dataBuffer, 0);
		return true;
	}

	public bool ReadQword(ulong addr, out long value)
	{
		value = 0L;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, 8, out bytesRead))
		{
			return false;
		}
		value = BitConverter.ToInt64(_dataBuffer, 0);
		return true;
	}

	public override bool ReadPointer(ulong addr, out ulong value)
	{
		int pointerSize = PointerSize;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, pointerSize, out bytesRead))
		{
			value = 3435973836uL;
			return false;
		}
		if (pointerSize == 4)
		{
			value = BitConverter.ToUInt32(_dataBuffer, 0);
		}
		else
		{
			value = BitConverter.ToUInt64(_dataBuffer, 0);
		}
		return true;
	}

	public bool ReadPtr(ulong addr, out long value)
	{
		int pointerSize = PointerSize;
		int bytesRead = 0;
		if (!ReadMemory(addr, _dataBuffer, pointerSize, out bytesRead))
		{
			value = 3435973836L;
			return false;
		}
		if (pointerSize == 4)
		{
			value = BitConverter.ToInt32(_dataBuffer, 0);
		}
		else
		{
			value = BitConverter.ToInt64(_dataBuffer, 0);
		}
		return true;
	}

	internal IEnumerable<ulong> GetPointersInRange(ulong start, ulong stop)
	{
		if (start >= stop)
		{
			return s_emptyPointerArray;
		}
		ulong num = (stop - start) / (ulong)IntPtr.Size;
		if (num > 4096)
		{
			return EnumeratePointersInRange(start, stop);
		}
		ulong[] array = new ulong[num];
		byte[] array2 = new byte[(int)num * IntPtr.Size];
		if (!ReadMemory(start, array2, array2.Length, out var _))
		{
			return s_emptyPointerArray;
		}
		if (IntPtr.Size == 4)
		{
			for (uint num2 = 0u; num2 < array.Length; num2++)
			{
				array[num2] = BitConverter.ToUInt32(array2, (int)(num2 * IntPtr.Size));
			}
		}
		else
		{
			for (uint num3 = 0u; num3 < array.Length; num3++)
			{
				array[num3] = BitConverter.ToUInt64(array2, (int)(num3 * IntPtr.Size));
			}
		}
		return array;
	}

	private IEnumerable<ulong> EnumeratePointersInRange(ulong start, ulong stop)
	{
		ulong value;
		for (ulong ptr = start; ptr < stop && ReadPointer(ptr, out value); ptr += (uint)IntPtr.Size)
		{
			yield return value;
		}
	}
}
