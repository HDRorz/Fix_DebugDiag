using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeRuntime : RuntimeBase
{
	private ISOSNative _sos;

	private NativeHeap _heap;

	private ClrThread[] _threads;

	private NativeModule[] _modules;

	private NativeAppDomain _domain;

	private int _dacRawVersion;

	public override int PointerSize => IntPtr.Size;

	public override IList<ClrAppDomain> AppDomains => new ClrAppDomain[1] { GetRhAppDomain() };

	public override IList<ClrThread> Threads
	{
		get
		{
			if (_threads == null)
			{
				InitThreads();
			}
			return _threads;
		}
	}

	internal NativeModule[] NativeModules
	{
		get
		{
			if (_modules != null)
			{
				return _modules;
			}
			List<ModuleInfo> list = new List<ModuleInfo>(DataTarget.EnumerateModules());
			list.Sort((ModuleInfo x, ModuleInfo y) => x.ImageBase.CompareTo(y.ImageBase));
			if (_sos.GetModuleList(0, null, out var pNeeded) < 0)
			{
				_modules = ConvertModuleList(list);
				return _modules;
			}
			ulong[] array = new ulong[pNeeded];
			if (_sos.GetModuleList(pNeeded, array, out pNeeded) < 0)
			{
				_modules = ConvertModuleList(list);
				return _modules;
			}
			Array.Sort(array);
			int num = 0;
			int num2 = 0;
			while (num < list.Count && num2 < array.Length)
			{
				ModuleInfo moduleInfo = list[num];
				ulong num3 = array[num2];
				if (moduleInfo.ImageBase <= num3 && num3 < moduleInfo.ImageBase + moduleInfo.FileSize)
				{
					num++;
					num2++;
				}
				else if (num3 < moduleInfo.ImageBase)
				{
					num2++;
				}
				else if (num3 >= moduleInfo.ImageBase + moduleInfo.FileSize)
				{
					list.RemoveAt(num);
				}
			}
			list.RemoveRange(num, list.Count - num);
			_modules = ConvertModuleList(list);
			return _modules;
		}
	}

	public NativeRuntime(DataTargetImpl dt, DacLibrary lib)
		: base(dt, lib)
	{
		byte[] array = new byte[4];
		if (!Request(3758096384u, null, array))
		{
			throw new ClrDiagnosticsException("Failed to request dac version.", ClrDiagnosticsException.HR.DacError);
		}
		_dacRawVersion = BitConverter.ToInt32(array, 0);
		if (_dacRawVersion != 10 && _dacRawVersion != 11)
		{
			throw new ClrDiagnosticsException("Unsupported dac version.", ClrDiagnosticsException.HR.DacError);
		}
	}

	protected override void InitApi()
	{
		if (_sos == null)
		{
			IXCLRDataProcess dacInterface = _library.DacInterface;
			if (!(dacInterface is ISOSNative))
			{
				throw new ClrDiagnosticsException("This version of mrt100 is too old.", ClrDiagnosticsException.HR.DataRequestError);
			}
			_sos = (ISOSNative)dacInterface;
		}
	}

	public override ClrHeap GetHeap()
	{
		if (_heap == null)
		{
			_heap = new NativeHeap(this, NativeModules, null);
		}
		return _heap;
	}

	public override ClrHeap GetHeap(TextWriter log)
	{
		if (_heap == null)
		{
			_heap = new NativeHeap(this, NativeModules, log);
		}
		else
		{
			_heap.Log = log;
		}
		return _heap;
	}

	public override IEnumerable<ClrHandle> EnumerateHandles()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<ClrMemoryRegion> EnumerateMemoryRegions()
	{
		throw new NotImplementedException();
	}

	public override ClrMethod GetMethodByAddress(ulong ip)
	{
		throw new NotImplementedException();
	}

	public override void Flush()
	{
		OnRuntimeFlushed();
		throw new NotImplementedException();
	}

	internal override IEnumerable<ClrStackFrame> EnumerateStackFrames(uint osThreadId)
	{
		throw new NotImplementedException();
	}

	public override ClrThreadPool GetThreadPool()
	{
		throw new NotImplementedException();
	}

	internal ClrAppDomain GetRhAppDomain()
	{
		if (_domain == null)
		{
			_domain = new NativeAppDomain(NativeModules);
		}
		return _domain;
	}

	private NativeModule[] ConvertModuleList(List<ModuleInfo> modules)
	{
		NativeModule[] array = new NativeModule[modules.Count];
		int num = 0;
		foreach (ModuleInfo module in modules)
		{
			array[num++] = new NativeModule(this, module);
		}
		return array;
	}

	internal unsafe IList<ClrRoot> EnumerateStackRoots(ClrThread thread)
	{
		int num = _dataReader.GetArchitecture() switch
		{
			Architecture.Amd64 => 1232, 
			Architecture.X86 => 720, 
			Architecture.Arm => 416, 
			_ => throw new InvalidOperationException("Unexpected architecture."), 
		};
		byte[] array = new byte[num];
		_dataReader.GetThreadContext(thread.OSThreadId, 0u, (uint)num, array);
		NativeStackRootWalker nativeStackRootWalker = new NativeStackRootWalker(GetHeap(), GetRhAppDomain(), thread);
		THREADROOTCALLBACK tHREADROOTCALLBACK = nativeStackRootWalker.Callback;
		IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(tHREADROOTCALLBACK);
		fixed (byte* value = &array[0])
		{
			IntPtr pInitialContext = new IntPtr(value);
			_sos.TraverseStackRoots(thread.Address, pInitialContext, num, functionPointerForDelegate, IntPtr.Zero);
		}
		GC.KeepAlive(tHREADROOTCALLBACK);
		return nativeStackRootWalker.Roots;
	}

	internal IList<ClrRoot> EnumerateStaticRoots(bool resolveStatics)
	{
		NativeStaticRootWalker nativeStaticRootWalker = new NativeStaticRootWalker(this, resolveStatics);
		STATICROOTCALLBACK sTATICROOTCALLBACK = nativeStaticRootWalker.Callback;
		IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(sTATICROOTCALLBACK);
		_sos.TraverseStaticRoots(functionPointerForDelegate);
		GC.KeepAlive(sTATICROOTCALLBACK);
		return nativeStaticRootWalker.Roots;
	}

	internal IEnumerable<ClrRoot> EnumerateHandleRoots()
	{
		NativeHandleRootWalker nativeHandleRootWalker = new NativeHandleRootWalker(this, _dacRawVersion != 10);
		HANDLECALLBACK hANDLECALLBACK = nativeHandleRootWalker.RootCallback;
		IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(hANDLECALLBACK);
		_sos.TraverseHandleTable(functionPointerForDelegate, IntPtr.Zero);
		GC.KeepAlive(hANDLECALLBACK);
		return nativeHandleRootWalker.Roots;
	}

	private void InitThreads()
	{
		IThreadStoreData threadStoreData = GetThreadStoreData();
		List<ClrThread> list = new List<ClrThread>(threadStoreData.Count);
		ulong num = threadStoreData.FirstThread;
		IThreadData thread = GetThread(threadStoreData.FirstThread);
		int num2 = 0;
		while (thread != null)
		{
			list.Add(new DesktopThread(this, thread, num, threadStoreData.Finalizer == num));
			num = thread.Next;
			thread = GetThread(num);
			num2++;
		}
		_threads = list.ToArray();
	}

	internal string ResolveSymbol(ulong eetype)
	{
		return DataTarget.ResolveSymbol(eetype);
	}

	internal override ClrAppDomain GetAppDomainByAddress(ulong addr)
	{
		return _domain;
	}

	internal override ulong GetFirstThread()
	{
		return GetThreadStoreData()?.FirstThread ?? 0;
	}

	internal override IThreadData GetThread(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		if (_sos.GetThreadData(addr, out var pThread) < 0)
		{
			return null;
		}
		return pThread;
	}

	internal override IHeapDetails GetSvrHeapDetails(ulong addr)
	{
		if (_sos.GetGCHeapDetails(addr, out var details) < 0)
		{
			return null;
		}
		return details;
	}

	internal override IHeapDetails GetWksHeapDetails()
	{
		if (_sos.GetGCHeapStaticData(out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override ulong[] GetServerHeapList()
	{
		int pNeeded = 0;
		if (_sos.GetGCHeapList(0, null, out pNeeded) < 0)
		{
			return null;
		}
		ulong[] array = new ulong[pNeeded];
		if (_sos.GetGCHeapList(array.Length, array, out pNeeded) < 0)
		{
			return null;
		}
		return array;
	}

	internal override IThreadStoreData GetThreadStoreData()
	{
		if (_sos.GetThreadStoreData(out var pData) < 0)
		{
			return null;
		}
		return pData;
	}

	internal override ISegmentData GetSegmentData(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		if (_sos.GetGCHeapSegment(addr, out var pSegmentData) < 0)
		{
			return null;
		}
		return pSegmentData;
	}

	internal override IMethodTableData GetMethodTableData(ulong eetype)
	{
		if (_sos.GetEETypeData(eetype, out var pData) < 0)
		{
			return null;
		}
		return pData;
	}

	internal ulong GetFreeType()
	{
		if (_sos.GetFreeEEType(out var freeType) < 0)
		{
			return 18446744073709551573uL;
		}
		return freeType;
	}

	internal override IGCInfo GetGCInfo()
	{
		if (_sos.GetGCHeapData(out var pData) < 0)
		{
			return null;
		}
		return pData;
	}

	internal override uint GetTlsSlot()
	{
		throw new NotImplementedException();
	}

	internal override uint GetThreadTypeIndex()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<int> EnumerateGCThreads()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<ClrModule> EnumerateModules()
	{
		throw new NotImplementedException();
	}

	public override CcwData GetCcwDataFromAddress(ulong addr)
	{
		throw new NotImplementedException();
	}
}
