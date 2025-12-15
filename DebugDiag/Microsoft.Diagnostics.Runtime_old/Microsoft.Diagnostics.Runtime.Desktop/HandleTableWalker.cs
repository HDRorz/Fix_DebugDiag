using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class HandleTableWalker
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int VISITHANDLEV4(ulong HandleAddr, ulong HandleValue, int HandleType, uint ulRefCount, ulong appDomainPtr, IntPtr token);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int VISITHANDLEV2(ulong HandleAddr, ulong HandleValue, int HandleType, ulong appDomainPtr, IntPtr token);

	private DesktopRuntimeBase _runtime;

	private ClrHeap _heap;

	private int _max = 10000;

	private VISITHANDLEV2 _mV2Delegate;

	private VISITHANDLEV4 _mV4Delegate;

	public List<ClrHandle> Handles { get; private set; }

	public byte[] V4Request
	{
		get
		{
			if (_mV4Delegate == null)
			{
				_mV4Delegate = VisitHandleV4;
			}
			IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(_mV4Delegate);
			byte[] array = new byte[IntPtr.Size * 2];
			FunctionPointerToByteArray(functionPointerForDelegate, array, 0);
			return array;
		}
	}

	public byte[] V2Request
	{
		get
		{
			if (_mV2Delegate == null)
			{
				_mV2Delegate = VisitHandleV2;
			}
			IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(_mV2Delegate);
			byte[] array = new byte[IntPtr.Size * 2];
			FunctionPointerToByteArray(functionPointerForDelegate, array, 0);
			return array;
		}
	}

	public HandleTableWalker(DesktopRuntimeBase dac)
	{
		_runtime = dac;
		_heap = dac.GetHeap();
		Handles = new List<ClrHandle>();
	}

	private int VisitHandleV4(ulong addr, ulong obj, int hndType, uint refCnt, ulong appDomain, IntPtr unused)
	{
		return AddHandle(addr, obj, hndType, refCnt, 0u, appDomain);
	}

	private int VisitHandleV2(ulong addr, ulong obj, int hndType, ulong appDomain, IntPtr unused)
	{
		uint refCnt = 0u;
		if ((long)hndType == 5)
		{
			refCnt = 1u;
		}
		return AddHandle(addr, obj, hndType, refCnt, 0u, appDomain);
	}

	public int AddHandle(ulong addr, ulong obj, int hndType, uint refCnt, uint dependentTarget, ulong appDomain)
	{
		if (!GetMethodTables(obj, out var _, out var _))
		{
			if (_max-- <= 0)
			{
				return 0;
			}
			return 1;
		}
		ClrHandle clrHandle = new ClrHandle();
		clrHandle.Address = addr;
		clrHandle.Object = obj;
		clrHandle.Type = _heap.GetObjectType(obj);
		clrHandle.HandleType = (HandleType)hndType;
		clrHandle.RefCount = refCnt;
		clrHandle.AppDomain = _runtime.GetAppDomainByAddress(appDomain);
		clrHandle.DependentTarget = dependentTarget;
		if (dependentTarget != 0)
		{
			clrHandle.DependentType = _heap.GetObjectType(dependentTarget);
		}
		Handles.Add(clrHandle);
		clrHandle = clrHandle.GetInteriorHandle();
		if (clrHandle != null)
		{
			Handles.Add(clrHandle);
		}
		if (_max-- <= 0)
		{
			return 0;
		}
		return 1;
	}

	private bool GetMethodTables(ulong obj, out ulong mt, out ulong cmt)
	{
		mt = 0uL;
		cmt = 0uL;
		byte[] array = new byte[IntPtr.Size * 3];
		int bytesRead = 0;
		if (!_runtime.ReadMemory(obj, array, array.Length, out bytesRead) || bytesRead != array.Length)
		{
			return false;
		}
		if (IntPtr.Size == 4)
		{
			mt = BitConverter.ToUInt32(array, 0);
		}
		else
		{
			mt = BitConverter.ToUInt64(array, 0);
		}
		if (mt == _runtime.ArrayMethodTable)
		{
			if (IntPtr.Size == 4)
			{
				cmt = BitConverter.ToUInt32(array, 2 * IntPtr.Size);
			}
			else
			{
				cmt = BitConverter.ToUInt64(array, 2 * IntPtr.Size);
			}
		}
		return true;
	}

	private static void FunctionPointerToByteArray(IntPtr functionPtr, byte[] request, int start)
	{
		long num = functionPtr.ToInt64();
		for (int i = start; i < start + 8; i++)
		{
			request[i] = (byte)num;
			num >>= 8;
		}
	}
}
