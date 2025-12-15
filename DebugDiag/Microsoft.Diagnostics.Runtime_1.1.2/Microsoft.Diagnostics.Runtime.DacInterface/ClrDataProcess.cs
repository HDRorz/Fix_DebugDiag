using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public class ClrDataProcess : CallableCOMWrapper
{
	private delegate int StartEnumMethodInstancesByAddressDelegate(IntPtr self, ulong address, IntPtr appDomain, out ulong handle);

	private delegate int EnumMethodInstanceByAddressDelegate(IntPtr self, ref ulong handle, out IntPtr method);

	private delegate int EndEnumMethodInstancesByAddressDelegate(IntPtr self, ulong handle);

	private delegate int RequestDelegate(IntPtr self, uint reqCode, uint inBufferSize, [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] inBuffer, uint outBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outBuffer);

	private delegate int FlushDelegate(IntPtr self);

	private delegate int GetTaskByOSThreadIDDelegate(IntPtr self, uint id, out IntPtr pUnknownTask);

	private static Guid IID_IXCLRDataProcess = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");

	private FlushDelegate _flush;

	private GetTaskByOSThreadIDDelegate _getTask;

	private RequestDelegate _request;

	private StartEnumMethodInstancesByAddressDelegate _startEnum;

	private EnumMethodInstanceByAddressDelegate _enum;

	private EndEnumMethodInstancesByAddressDelegate _endEnum;

	private readonly DacLibrary _library;

	private unsafe IXCLRDataProcessVtable* VTable => (IXCLRDataProcessVtable*)base._vtable;

	public ClrDataProcess(DacLibrary library, IntPtr pUnknown)
		: base(library.OwningLibrary, ref IID_IXCLRDataProcess, pUnknown)
	{
		_library = library;
	}

	public ClrDataProcess(CallableCOMWrapper toClone)
		: base(toClone)
	{
	}

	public SOSDac GetSOSDacInterface()
	{
		IntPtr intPtr = QueryInterface(ref SOSDac.IID_ISOSDac);
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			return new SOSDac(_library, intPtr);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public SOSDac6 GetSOSDacInterface6()
	{
		IntPtr intPtr = QueryInterface(ref SOSDac6.IID_ISOSDac6);
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			return new SOSDac6(_library, intPtr);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public unsafe void Flush()
	{
		CallableCOMWrapper.InitDelegate(ref _flush, VTable->Flush);
		_flush(base.Self);
	}

	public unsafe int Request(uint reqCode, uint inBufferSize, byte[] inBuffer, uint outBufferSize, byte[] outBuffer)
	{
		CallableCOMWrapper.InitDelegate(ref _request, VTable->Request);
		return _request(base.Self, reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
	}

	public unsafe ClrStackWalk CreateStackWalk(uint id, uint flags)
	{
		CallableCOMWrapper.InitDelegate(ref _getTask, VTable->GetTaskByOSThreadID);
		if (_getTask(base.Self, id, out var pUnknownTask) != 0)
		{
			return null;
		}
		using ClrDataTask clrDataTask = new ClrDataTask(_library, pUnknownTask);
		int num = AddRef();
		ClrStackWalk clrStackWalk = clrDataTask.CreateStackWalk(_library, flags);
		if (Release() == num && clrStackWalk == null)
		{
			Release();
		}
		return clrStackWalk;
	}

	public unsafe IEnumerable<ClrDataMethod> EnumerateMethodInstancesByAddress(ulong addr)
	{
		CallableCOMWrapper.InitDelegate(ref _startEnum, VTable->StartEnumMethodInstancesByAddress);
		CallableCOMWrapper.InitDelegate(ref _enum, VTable->EnumMethodInstanceByAddress);
		CallableCOMWrapper.InitDelegate(ref _endEnum, VTable->EndEnumMethodInstancesByAddress);
		List<ClrDataMethod> list = new List<ClrDataMethod>(1);
		if (_startEnum(base.Self, addr, IntPtr.Zero, out var handle) != 0)
		{
			return list;
		}
		try
		{
			IntPtr method;
			while (_enum(base.Self, ref handle, out method) == 0)
			{
				list.Add(new ClrDataMethod(_library, method));
			}
			return list;
		}
		finally
		{
			_endEnum(base.Self, handle);
		}
	}
}
