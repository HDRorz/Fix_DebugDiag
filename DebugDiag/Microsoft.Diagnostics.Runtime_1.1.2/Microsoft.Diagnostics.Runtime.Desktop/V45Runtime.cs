using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class V45Runtime : DesktopRuntimeBase
{
	private List<ClrHandle> _handles;

	private SOSDac _sos;

	private SOSDac6 _sos6;

	internal override DesktopVersion CLRVersion => DesktopVersion.v45;

	public V45Runtime(ClrInfo info, DataTarget dt, DacLibrary lib)
		: base(info, dt, lib)
	{
		if (!GetCommonMethodTables(ref _commonMTs))
		{
			throw new ClrDiagnosticsException("Could not request common MethodTable list.", ClrDiagnosticsExceptionKind.DacError);
		}
		if (!_commonMTs.Validate())
		{
			base.CanWalkHeap = false;
		}
		byte[] array = new byte[4];
		if (!Request(3758096384u, null, array))
		{
			throw new ClrDiagnosticsException("Failed to request dac version.", ClrDiagnosticsExceptionKind.DacError);
		}
		if (BitConverter.ToInt32(array, 0) != 9)
		{
			throw new ClrDiagnosticsException("Unsupported dac version.", ClrDiagnosticsExceptionKind.DacError);
		}
	}

	protected override void InitApi()
	{
		if (_sos == null)
		{
			_sos = base.DacLibrary.GetSOSInterfaceNoAddRef();
		}
		if (_sos6 == null)
		{
			_sos6 = base.DacLibrary.GetSOSInterface6NoAddRef();
		}
	}

	public override IEnumerable<ClrHandle> EnumerateHandles()
	{
		if (_handles != null)
		{
			return _handles;
		}
		return EnumerateHandleWorker();
	}

	private IEnumerable<ClrHandle> EnumerateHandleWorker()
	{
		List<ClrHandle> result = new List<ClrHandle>();
		using (SOSHandleEnum handleEnum = _sos.EnumerateHandles())
		{
			HandleData[] handles = new HandleData[8];
			while (true)
			{
				int num;
				int fetched = (num = handleEnum.ReadHandles(handles));
				if (num == 0)
				{
					break;
				}
				for (int i = 0; i < fetched; i++)
				{
					ClrHandle handle = new ClrHandle(this, Heap, handles[i]);
					result.Add(handle);
					yield return handle;
					handle = handle.GetInteriorHandle();
					if (handle != null)
					{
						result.Add(handle);
						yield return handle;
					}
				}
			}
		}
		_handles = result;
	}

	internal override Dictionary<ulong, List<ulong>> GetDependentHandleMap(CancellationToken cancelToken)
	{
		Dictionary<ulong, List<ulong>> dictionary = new Dictionary<ulong, List<ulong>>();
		using SOSHandleEnum sOSHandleEnum = _sos.EnumerateHandles();
		if (sOSHandleEnum == null)
		{
			return dictionary;
		}
		HandleData[] array = new HandleData[32];
		int num;
		while ((num = sOSHandleEnum.ReadHandles(array)) != 0)
		{
			for (int i = 0; i < num; i++)
			{
				cancelToken.ThrowIfCancellationRequested();
				if (array[i].Type == 6 && ReadPointer(array[i].Handle, out var value))
				{
					if (!dictionary.TryGetValue(value, out var value2))
					{
						value2 = (dictionary[value] = new List<ulong>());
					}
					value2.Add(array[i].Secondary);
				}
			}
		}
		return dictionary;
	}

	internal override IEnumerable<ClrRoot> EnumerateStackReferences(ClrThread thread, bool includeDead)
	{
		if (includeDead)
		{
			return base.EnumerateStackReferences(thread, includeDead);
		}
		return EnumerateStackReferencesWorker(thread);
	}

	private IEnumerable<ClrRoot> EnumerateStackReferencesWorker(ClrThread thread)
	{
		using SOSStackRefEnum stackRefEnum = _sos.EnumerateStackRefs(thread.OSThreadId);
		if (stackRefEnum == null)
		{
			yield break;
		}
		ClrAppDomain domain = GetAppDomainByAddress(thread.AppDomain);
		ClrHeap heap = Heap;
		StackRefData[] refs = new StackRefData[1024];
		while (true)
		{
			int num;
			int fetched = (num = stackRefEnum.ReadStackReferences(refs));
			if (num == 0)
			{
				break;
			}
			uint i = 0u;
			while (i < fetched && i < refs.Length)
			{
				if (refs[i].Object != 0L)
				{
					bool pinned = (refs[i].Flags & 2) == 2;
					bool flag = (refs[i].Flags & 1) == 1;
					ClrType clrType = null;
					if (!flag)
					{
						clrType = heap.GetObjectType(refs[i].Object);
					}
					ClrStackFrame stackFrame = thread.StackTrace.SingleOrDefault((ClrStackFrame f) => f.StackPointer == refs[i].Source || (f.StackPointer == refs[i].StackPointer && f.InstructionPointer == refs[i].Source));
					if (flag || clrType != null)
					{
						yield return new LocalVarRoot(refs[i].Address, refs[i].Object, clrType, domain, thread, pinned, falsePos: false, flag, stackFrame);
					}
				}
				uint num2 = i + 1;
				i = num2;
			}
		}
	}

	internal override ulong GetFirstThread()
	{
		return GetThreadStoreData()?.FirstThread ?? 0;
	}

	internal override IThreadData GetThread(ulong addr)
	{
		if (_sos.GetThreadData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IHeapDetails GetSvrHeapDetails(ulong addr)
	{
		if (_sos.GetServerHeapDetails(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IHeapDetails GetWksHeapDetails()
	{
		if (_sos.GetWksHeapDetails(out var data))
		{
			return data;
		}
		return null;
	}

	internal override ulong[] GetServerHeapList()
	{
		return _sos.GetHeapList(base.HeapCount);
	}

	internal override ulong[] GetAppDomainList(int count)
	{
		return _sos.GetAppDomainList(count);
	}

	internal override ulong[] GetAssemblyList(ulong appDomain, int count)
	{
		return _sos.GetAssemblyList(appDomain, count);
	}

	internal override ulong[] GetModuleList(ulong assembly, int count)
	{
		return _sos.GetModuleList(assembly, count);
	}

	internal override IAssemblyData GetAssemblyData(ulong domain, ulong assembly)
	{
		if (_sos.GetAssemblyData(domain, assembly, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IAppDomainStoreData GetAppDomainStoreData()
	{
		if (_sos.GetAppDomainStoreData(out var data))
		{
			return data;
		}
		return null;
	}

	internal override IMethodTableData GetMethodTableData(ulong addr)
	{
		if ((addr & 2) == 2)
		{
			return null;
		}
		if (_sos.GetMethodTableData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IMethodTableCollectibleData GetMethodTableCollectibleData(ulong addr)
	{
		if (_sos6 != null && _sos6.GetMethodTableCollectibleData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override ulong GetMethodTableByEEClass(ulong eeclass)
	{
		return _sos.GetMethodTableByEEClass(eeclass);
	}

	internal override IGCInfo GetGCInfoImpl()
	{
		if (_sos.GetGcHeapData(out var data))
		{
			return data;
		}
		return null;
	}

	internal override bool GetCommonMethodTables(ref CommonMethodTables mts)
	{
		return _sos.GetCommonMethodTables(out mts);
	}

	public override string GetMethodTableName(ulong mt)
	{
		return _sos.GetMethodTableName(mt);
	}

	internal override string GetPEFileName(ulong addr)
	{
		return _sos.GetPEFileName(addr);
	}

	internal override IModuleData GetModuleData(ulong addr)
	{
		if (_sos.GetModuleData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override ulong GetModuleForMT(ulong addr)
	{
		if (_sos.GetMethodTableData(addr, out var data))
		{
			return data.Module;
		}
		return 0uL;
	}

	internal override ISegmentData GetSegmentData(ulong addr)
	{
		if (_sos.GetSegmentData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IAppDomainData GetAppDomainData(ulong addr)
	{
		if (_sos.GetAppDomainData(addr, out var data))
		{
			return data;
		}
		return null;
	}

	internal override string GetAppDomaminName(ulong addr)
	{
		return _sos.GetAppDomainName(addr);
	}

	internal override string GetAssemblyName(ulong addr)
	{
		return _sos.GetAssemblyName(addr);
	}

	internal override bool TraverseHeap(ulong heap, SOSDac.LoaderHeapTraverse callback)
	{
		return _sos.TraverseLoaderHeap(heap, callback);
	}

	internal override bool TraverseStubHeap(ulong appDomain, int type, SOSDac.LoaderHeapTraverse callback)
	{
		return _sos.TraverseStubHeap(appDomain, type, callback);
	}

	internal override IEnumerable<ICodeHeap> EnumerateJitHeaps()
	{
		JitManagerInfo[] jitManagers = _sos.GetJitManagers();
		int i = 0;
		while (i < jitManagers.Length)
		{
			int num;
			if (jitManagers[i].Type == CodeHeapType.Unknown)
			{
				JitCodeHeapInfo[] heapInfo = _sos.GetCodeHeapList(jitManagers[i].Address);
				for (int j = 0; j < heapInfo.Length; j = num)
				{
					yield return heapInfo[i];
					num = j + 1;
				}
			}
			num = i + 1;
			i = num;
		}
	}

	internal override IFieldInfo GetFieldInfo(ulong mt)
	{
		if (_sos.GetFieldInfo(mt, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IFieldData GetFieldData(ulong fieldDesc)
	{
		if (_sos.GetFieldData(fieldDesc, out var data))
		{
			return data;
		}
		return null;
	}

	internal override MetaDataImport GetMetadataImport(ulong module)
	{
		return _sos.GetMetadataImport(module);
	}

	internal override IObjectData GetObjectData(ulong objRef)
	{
		if (_sos.GetObjectData(objRef, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IList<MethodTableTokenPair> GetMethodTableList(ulong module)
	{
		List<MethodTableTokenPair> mts = new List<MethodTableTokenPair>();
		_sos.TraverseModuleMap(SOSDac.ModuleMapTraverseKind.TypeDefToMethodTable, module, delegate(uint index, ulong mt, IntPtr token)
		{
			mts.Add(new MethodTableTokenPair(mt, index));
		});
		return mts;
	}

	internal override IDomainLocalModuleData GetDomainLocalModuleById(ulong appDomain, ulong id)
	{
		if (_sos.GetDomainLocalModuleDataFromAppDomain(appDomain, (int)id, out var data))
		{
			return data;
		}
		return null;
	}

	internal override COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count)
	{
		return _sos.GetCCWInterfaces(ccw, count);
	}

	internal override COMInterfacePointerData[] GetRCWInterfaces(ulong rcw, int count)
	{
		return _sos.GetRCWInterfaces(rcw, count);
	}

	internal override ICCWData GetCCWData(ulong ccw)
	{
		if (_sos.GetCCWData(ccw, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IRCWData GetRCWData(ulong rcw)
	{
		if (_sos.GetRCWData(rcw, out var data))
		{
			return data;
		}
		return null;
	}

	internal override ulong GetILForModule(ClrModule module, uint rva)
	{
		return _sos.GetILForModule(module.Address, rva);
	}

	internal override ulong GetThreadStaticPointer(ulong thread, ClrElementType type, uint offset, uint moduleId, bool shared)
	{
		ulong num = offset;
		if (!_sos.GetThreadLocalModuleData(thread, moduleId, out var data))
		{
			return 0uL;
		}
		if (type.IsObjectReference() || type.IsValueClass())
		{
			return num + data.GCStaticDataStart;
		}
		return num + data.NonGCStaticDataStart;
	}

	internal override IDomainLocalModuleData GetDomainLocalModule(ulong appDomain, ulong module)
	{
		if (_sos.GetDomainLocalModuleDataFromModule(module, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IList<ulong> GetMethodDescList(ulong methodTable)
	{
		if (!_sos.GetMethodTableData(methodTable, out var data))
		{
			return null;
		}
		uint numMethods = data.NumMethods;
		ulong[] array = new ulong[numMethods];
		for (int i = 0; i < numMethods; i++)
		{
			if (_sos.GetCodeHeaderData(_sos.GetMethodTableSlot(methodTable, i), out var codeHeaderData))
			{
				array[i] = codeHeaderData.MethodDesc;
			}
		}
		return array;
	}

	internal override string GetNameForMD(ulong md)
	{
		return _sos.GetMethodDescName(md);
	}

	internal override IMethodDescData GetMethodDescData(ulong md)
	{
		V45MethodDescDataWrapper v45MethodDescDataWrapper = new V45MethodDescDataWrapper();
		if (!v45MethodDescDataWrapper.Init(_sos, md))
		{
			return null;
		}
		return v45MethodDescDataWrapper;
	}

	internal override uint GetMetadataToken(ulong mt)
	{
		if (!_sos.GetMethodTableData(mt, out var data))
		{
			return uint.MaxValue;
		}
		return data.Token;
	}

	protected override DesktopStackFrame GetStackFrame(DesktopThread thread, byte[] context, ulong ip, ulong framePtr, ulong frameVtbl)
	{
		if (frameVtbl != 0L)
		{
			ClrMethod innerMethod = null;
			string frameName = _sos.GetFrameName(frameVtbl);
			ulong methodDescPtrFromFrame = _sos.GetMethodDescPtrFromFrame(framePtr);
			if (methodDescPtrFromFrame != 0L)
			{
				V45MethodDescDataWrapper v45MethodDescDataWrapper = new V45MethodDescDataWrapper();
				if (v45MethodDescDataWrapper.Init(_sos, methodDescPtrFromFrame))
				{
					innerMethod = DesktopMethod.Create(this, v45MethodDescDataWrapper);
				}
			}
			return new DesktopStackFrame(this, thread, context, framePtr, frameName, innerMethod);
		}
		return new DesktopStackFrame(this, thread, context, ip, framePtr, _sos.GetMethodDescPtrFromIP(ip));
	}

	private bool GetStackTraceFromField(ClrType type, ulong obj, out ulong stackTrace)
	{
		stackTrace = 0uL;
		ClrInstanceField fieldByName = type.GetFieldByName("_stackTrace");
		if (fieldByName == null)
		{
			return false;
		}
		object value = fieldByName.GetValue(obj);
		if (value == null || !(value is ulong))
		{
			return false;
		}
		stackTrace = (ulong)value;
		return true;
	}

	internal override IList<ClrStackFrame> GetExceptionStackTrace(ulong obj, ClrType type)
	{
		List<ClrStackFrame> list = new List<ClrStackFrame>();
		if (type == null)
		{
			return list;
		}
		if (!GetStackTraceFromField(type, obj, out var stackTrace) && !ReadPointer(obj + GetStackTraceOffset(), out stackTrace))
		{
			return list;
		}
		if (stackTrace == 0L)
		{
			return list;
		}
		ClrType objectType = Heap.GetObjectType(stackTrace);
		if (objectType == null || !objectType.IsArray)
		{
			return list;
		}
		if (objectType.GetArrayLength(stackTrace) == 0)
		{
			return list;
		}
		int num = IntPtr.Size * 4;
		ulong num2 = stackTrace + (ulong)(IntPtr.Size * 2);
		if (!ReadPointer(num2, out var value))
		{
			return list;
		}
		num2 += (ulong)(IntPtr.Size * 2);
		DesktopThread desktopThread = null;
		for (int i = 0; i < (int)value; i++)
		{
			if (!ReadPointer(num2, out var value2))
			{
				break;
			}
			if (!ReadPointer(num2 + (ulong)IntPtr.Size, out var value3))
			{
				break;
			}
			if (!ReadPointer(num2 + (ulong)(2 * IntPtr.Size), out var value4))
			{
				break;
			}
			if (i == 0 && value3 != 0L)
			{
				desktopThread = (DesktopThread)GetThreadByStackAddress(value3);
			}
			if (i == 1 && desktopThread == null && value3 != 0L)
			{
				desktopThread = (DesktopThread)GetThreadByStackAddress(value3);
			}
			list.Add(new DesktopStackFrame(this, desktopThread, null, value2, value3, value4));
			num2 += (ulong)num;
		}
		return list;
	}

	internal override IThreadStoreData GetThreadStoreData()
	{
		if (!_sos.GetThreadStoreData(out var data))
		{
			return null;
		}
		return data;
	}

	internal override string GetAppBase(ulong appDomain)
	{
		return _sos.GetAppBase(appDomain);
	}

	internal override string GetConfigFile(ulong appDomain)
	{
		return _sos.GetConfigFile(appDomain);
	}

	internal override IMethodDescData GetMDForIP(ulong ip)
	{
		ulong num = _sos.GetMethodDescPtrFromIP(ip);
		if (num == 0L)
		{
			if (!_sos.GetCodeHeaderData(ip, out var codeHeaderData))
			{
				return null;
			}
			if ((num = codeHeaderData.MethodDesc) == 0L)
			{
				return null;
			}
		}
		V45MethodDescDataWrapper v45MethodDescDataWrapper = new V45MethodDescDataWrapper();
		if (!v45MethodDescDataWrapper.Init(_sos, num))
		{
			return null;
		}
		return v45MethodDescDataWrapper;
	}

	protected override ulong GetThreadFromThinlock(uint threadId)
	{
		return _sos.GetThreadFromThinlockId(threadId);
	}

	internal override int GetSyncblkCount()
	{
		if (_sos.GetSyncBlockData(1, out var data))
		{
			return (int)data.TotalSyncBlockCount;
		}
		return 0;
	}

	internal override ISyncBlkData GetSyncblkData(int index)
	{
		if (_sos.GetSyncBlockData(index + 1, out var data))
		{
			return data;
		}
		return null;
	}

	internal override IThreadPoolData GetThreadPoolData()
	{
		if (_sos.GetThreadPoolData(out var data))
		{
			return data;
		}
		return null;
	}

	internal override uint GetTlsSlot()
	{
		return _sos.GetTlsIndex();
	}

	internal override uint GetThreadTypeIndex()
	{
		return 11u;
	}

	protected override uint GetRWLockDataOffset()
	{
		if (PointerSize == 8)
		{
			return 48u;
		}
		return 24u;
	}

	internal override IEnumerable<NativeWorkItem> EnumerateWorkItems()
	{
		ulong num = GetThreadPoolData().FirstWorkRequest;
		WorkRequestData requestData;
		while (num != 0L && _sos.GetWorkRequestData(num, out requestData))
		{
			yield return new DesktopNativeWorkItem(requestData);
			num = requestData.NextWorkRequest;
		}
	}

	internal override uint GetStringFirstCharOffset()
	{
		if (PointerSize == 8)
		{
			return 12u;
		}
		return 8u;
	}

	internal override uint GetStringLengthOffset()
	{
		if (PointerSize == 8)
		{
			return 8u;
		}
		return 4u;
	}

	internal override uint GetExceptionHROffset()
	{
		if (PointerSize != 8)
		{
			return 64u;
		}
		return 140u;
	}

	public override string GetJitHelperFunctionName(ulong addr)
	{
		return _sos.GetJitHelperFunctionName(addr);
	}
}
