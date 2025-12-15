using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class V45Runtime : DesktopRuntimeBase
{
	private ISOSDac _sos;

	internal override DesktopVersion CLRVersion => DesktopVersion.v45;

	public V45Runtime(DataTargetImpl dt, DacLibrary lib)
		: base(dt, lib)
	{
		if (!GetCommonMethodTables(ref _commonMTs))
		{
			throw new ClrDiagnosticsException("Could not request common MethodTable list.", ClrDiagnosticsException.HR.DacError);
		}
		byte[] array = new byte[4];
		if (!Request(3758096384u, null, array))
		{
			throw new ClrDiagnosticsException("Failed to request dac version.", ClrDiagnosticsException.HR.DacError);
		}
		if (BitConverter.ToInt32(array, 0) != 9)
		{
			throw new ClrDiagnosticsException("Unsupported dac version.", ClrDiagnosticsException.HR.DacError);
		}
	}

	protected override void InitApi()
	{
		if (_sos == null)
		{
			_sos = _library.SOSInterface;
		}
	}

	public override IEnumerable<ClrHandle> EnumerateHandles()
	{
		if (_sos.GetHandleEnum(out var ppHandleEnum) < 0)
		{
			return null;
		}
		if (!(ppHandleEnum is ISOSHandleEnum iSOSHandleEnum))
		{
			return null;
		}
		HandleData[] array = new HandleData[1];
		List<ClrHandle> list = new List<ClrHandle>();
		uint pNeeded = 0u;
		while (iSOSHandleEnum.Next(1u, array, out pNeeded) == 0)
		{
			ClrHandle clrHandle = new ClrHandle(this, GetHeap(), array[0]);
			list.Add(clrHandle);
			clrHandle = clrHandle.GetInteriorHandle();
			if (clrHandle != null)
			{
				list.Add(clrHandle);
			}
			if (pNeeded != 1)
			{
				break;
			}
		}
		return list;
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
		ISOSStackRefEnum handleEnum = null;
		if (_sos.GetStackReferences(thread.OSThreadId, out var ppEnum) >= 0)
		{
			handleEnum = ppEnum as ISOSStackRefEnum;
		}
		ClrAppDomain domain = GetAppDomainByAddress(thread.AppDomain);
		if (handleEnum == null)
		{
			yield break;
		}
		ClrHeap heap = GetHeap();
		StackRefData[] refs = new StackRefData[1024];
		uint fetched = 0u;
		while (handleEnum.Next((uint)refs.Length, refs, out fetched) >= 0)
		{
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
					if (flag || clrType != null)
					{
						yield return new LocalVarRoot(refs[i].Address, refs[i].Object, clrType, domain, thread, pinned, falsePos: false, flag);
					}
				}
				uint num = i + 1;
				i = num;
			}
			if (fetched != refs.Length)
			{
				break;
			}
		}
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
		if (_sos.GetThreadData(addr, out var data) < 0)
		{
			return null;
		}
		return data;
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
		ulong[] array = new ulong[base.HeapCount];
		if (_sos.GetGCHeapList((uint)base.HeapCount, array, out var _) < 0)
		{
			return null;
		}
		return array;
	}

	internal override IList<ulong> GetAppDomainList(int count)
	{
		ulong[] array = new ulong[1024];
		if (_sos.GetAppDomainList((uint)array.Length, array, out var pNeeded) < 0)
		{
			return null;
		}
		List<ulong> list = new List<ulong>((int)pNeeded);
		for (uint num = 0u; num < pNeeded; num++)
		{
			list.Add(array[num]);
		}
		return list;
	}

	internal override ulong[] GetAssemblyList(ulong appDomain, int count)
	{
		if (appDomain == base.SystemDomainAddress)
		{
			return new ulong[0];
		}
		if (_sos.GetAssemblyList(appDomain, 0, null, out var pNeeded) < 0)
		{
			return null;
		}
		ulong[] array = new ulong[pNeeded];
		if (_sos.GetAssemblyList(appDomain, pNeeded, array, out pNeeded) < 0)
		{
			return null;
		}
		return array;
	}

	internal override ulong[] GetModuleList(ulong assembly, int count)
	{
		if (_sos.GetAssemblyModuleList(assembly, 0u, null, out var pNeeded) < 0)
		{
			return null;
		}
		ulong[] array = new ulong[pNeeded];
		if (_sos.GetAssemblyModuleList(assembly, pNeeded, array, out pNeeded) < 0)
		{
			return null;
		}
		return array;
	}

	internal override IAssemblyData GetAssemblyData(ulong domain, ulong assembly)
	{
		if (_sos.GetAssemblyData(domain, assembly, out var data) < 0 && data.Address != assembly)
		{
			return null;
		}
		return data;
	}

	internal override IAppDomainStoreData GetAppDomainStoreData()
	{
		if (_sos.GetAppDomainStoreData(out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IMethodTableData GetMethodTableData(ulong addr)
	{
		if (_sos.GetMethodTableData(addr, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IGCInfo GetGCInfo()
	{
		if (_sos.GetGCHeapData(out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override bool GetCommonMethodTables(ref CommonMethodTables mCommonMTs)
	{
		return _sos.GetUsefulGlobals(out mCommonMTs) >= 0;
	}

	internal override string GetNameForMT(ulong mt)
	{
		if (_sos.GetMethodTableName(mt, 0u, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Capacity = (int)pNeeded;
		if (_sos.GetMethodTableName(mt, pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override string GetPEFileName(ulong addr)
	{
		if (_sos.GetPEFileName(addr, 0u, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder((int)pNeeded);
		if (_sos.GetPEFileName(addr, pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override IModuleData GetModuleData(ulong addr)
	{
		if (_sos.GetModuleData(addr, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override ulong GetModuleForMT(ulong addr)
	{
		if (_sos.GetMethodTableData(addr, out var data) < 0)
		{
			return 0uL;
		}
		return data.module;
	}

	internal override ISegmentData GetSegmentData(ulong addr)
	{
		if (_sos.GetHeapSegmentData(addr, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IAppDomainData GetAppDomainData(ulong addr)
	{
		if (_sos.GetAppDomainData(addr, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override string GetAppDomaminName(ulong addr)
	{
		if (_sos.GetAppDomainName(addr, 0u, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Capacity = (int)pNeeded;
		if (_sos.GetAppDomainName(addr, pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override string GetAssemblyName(ulong addr)
	{
		if (_sos.GetAssemblyName(addr, 0u, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Capacity = (int)pNeeded;
		if (_sos.GetAssemblyName(addr, pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override bool TraverseHeap(ulong heap, LoaderHeapTraverse callback)
	{
		bool result = _sos.TraverseLoaderHeap(heap, Marshal.GetFunctionPointerForDelegate(callback)) >= 0;
		GC.KeepAlive(callback);
		return result;
	}

	internal override bool TraverseStubHeap(ulong appDomain, int type, LoaderHeapTraverse callback)
	{
		bool result = _sos.TraverseVirtCallStubHeap(appDomain, (uint)type, Marshal.GetFunctionPointerForDelegate(callback)) >= 0;
		GC.KeepAlive(callback);
		return result;
	}

	internal override IEnumerable<ICodeHeap> EnumerateJitHeaps()
	{
		LegacyJitManagerInfo[] jitManagers = null;
		uint pNeeded = 0u;
		int jitManagerList = _sos.GetJitManagerList(0u, null, out pNeeded);
		if (jitManagerList >= 0)
		{
			jitManagers = new LegacyJitManagerInfo[pNeeded];
			jitManagerList = _sos.GetJitManagerList(pNeeded, jitManagers, out pNeeded);
		}
		if (jitManagerList < 0 || jitManagers == null)
		{
			yield break;
		}
		int i = 0;
		while (i < jitManagers.Length)
		{
			int num;
			if (jitManagers[i].type == CodeHeapType.Unknown)
			{
				jitManagerList = _sos.GetCodeHeapList(jitManagers[i].addr, 0u, null, out pNeeded);
				if (jitManagerList >= 0 && pNeeded != 0)
				{
					LegacyJitCodeHeapInfo[] heapInfo = new LegacyJitCodeHeapInfo[pNeeded];
					jitManagerList = _sos.GetCodeHeapList(jitManagers[i].addr, pNeeded, heapInfo, out pNeeded);
					if (jitManagerList >= 0)
					{
						for (int j = 0; j < heapInfo.Length; j = num)
						{
							yield return heapInfo[i];
							num = j + 1;
						}
					}
				}
			}
			num = i + 1;
			i = num;
		}
	}

	internal override IFieldInfo GetFieldInfo(ulong mt)
	{
		if (_sos.GetMethodTableFieldData(mt, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IFieldData GetFieldData(ulong fieldDesc)
	{
		if (_sos.GetFieldDescData(fieldDesc, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IMetadata GetMetadataImport(ulong module)
	{
		object module2 = null;
		if (module == 0L || _sos.GetModule(module, out module2) < 0)
		{
			return null;
		}
		return module2 as IMetadata;
	}

	internal override IObjectData GetObjectData(ulong objRef)
	{
		if (_sos.GetObjectData(objRef, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IList<ulong> GetMethodTableList(ulong module)
	{
		List<ulong> mts = new List<ulong>();
		if (_sos.TraverseModuleMap(0, module, delegate(uint index, ulong mt, IntPtr token)
		{
			mts.Add(mt);
		}, IntPtr.Zero) >= 0)
		{
			return mts;
		}
		return null;
	}

	internal override IDomainLocalModuleData GetDomainLocalModule(ulong appDomain, ulong id)
	{
		if (_sos.GetDomainLocalModuleDataFromAppDomain(appDomain, (int)id, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count)
	{
		COMInterfacePointerData[] array = new COMInterfacePointerData[count];
		if (_sos.GetCCWInterfaces(ccw, (uint)count, array, out var _) >= 0)
		{
			return array;
		}
		return null;
	}

	internal override COMInterfacePointerData[] GetRCWInterfaces(ulong rcw, int count)
	{
		COMInterfacePointerData[] array = new COMInterfacePointerData[count];
		if (_sos.GetRCWInterfaces(rcw, (uint)count, array, out var _) >= 0)
		{
			return array;
		}
		return null;
	}

	internal override ICCWData GetCCWData(ulong ccw)
	{
		if (ccw != 0L && _sos.GetCCWData(ccw, out var data) >= 0)
		{
			return data;
		}
		return null;
	}

	internal override IRCWData GetRCWData(ulong rcw)
	{
		if (rcw != 0L && _sos.GetRCWData(rcw, out var data) >= 0)
		{
			return data;
		}
		return null;
	}

	internal override ulong GetThreadStaticPointer(ulong thread, ClrElementType type, uint offset, uint moduleId, bool shared)
	{
		ulong num = offset;
		if (_sos.GetThreadLocalModuleData(thread, moduleId, out var data) < 0)
		{
			return 0uL;
		}
		if (ClrRuntime.IsObjectReference(type) || ClrRuntime.IsValueClass(type))
		{
			return num + data.pGCStaticDataStart;
		}
		return num + data.pNonGCStaticDataStart;
	}

	internal override IDomainLocalModuleData GetDomainLocalModule(ulong module)
	{
		if (_sos.GetDomainLocalModuleDataFromModule(module, out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IList<ulong> GetMethodDescList(ulong methodTable)
	{
		if (_sos.GetMethodTableData(methodTable, out var data) < 0)
		{
			return null;
		}
		List<ulong> list = new List<ulong>(data.wNumMethods);
		ulong value = 0uL;
		for (uint num = 0u; num < data.wNumMethods; num++)
		{
			if (_sos.GetMethodTableSlot(methodTable, num, out value) >= 0 && _sos.GetCodeHeaderData(value, out var data2) >= 0)
			{
				list.Add(data2.MethodDescPtr);
			}
		}
		return list;
	}

	internal override string GetNameForMD(ulong md)
	{
		StringBuilder stringBuilder = new StringBuilder();
		uint pNeeded = 0u;
		if (_sos.GetMethodDescName(md, 0u, null, out pNeeded) < 0)
		{
			return "UNKNOWN";
		}
		stringBuilder.Capacity = (int)pNeeded;
		if (_sos.GetMethodDescName(md, (uint)stringBuilder.Capacity, stringBuilder, out pNeeded) < 0)
		{
			return "UNKNOWN";
		}
		return stringBuilder.ToString();
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
		if (_sos.GetMethodTableData(mt, out var data) < 0)
		{
			return uint.MaxValue;
		}
		return data.token;
	}

	protected override DesktopStackFrame GetStackFrame(int res, ulong ip, ulong framePtr, ulong frameVtbl)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Capacity = 256;
		if (res >= 0 && frameVtbl != 0L)
		{
			ClrMethod innerMethod = null;
			string method = "Unknown Frame";
			if (_sos.GetFrameName(frameVtbl, (uint)stringBuilder.Capacity, stringBuilder, out var _) >= 0)
			{
				method = stringBuilder.ToString();
			}
			ulong ppMD = 0uL;
			if (_sos.GetMethodDescPtrFromFrame(framePtr, out ppMD) == 0)
			{
				V45MethodDescDataWrapper v45MethodDescDataWrapper = new V45MethodDescDataWrapper();
				if (v45MethodDescDataWrapper.Init(_sos, ppMD))
				{
					innerMethod = DesktopMethod.Create(this, v45MethodDescDataWrapper);
				}
			}
			return new DesktopStackFrame(this, framePtr, method, innerMethod);
		}
		if (_sos.GetMethodDescPtrFromIP(ip, out var ppMD2) >= 0)
		{
			return new DesktopStackFrame(this, ip, framePtr, ppMD2);
		}
		return new DesktopStackFrame(this, ip, framePtr, 0uL);
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
		ClrType objectType = GetHeap().GetObjectType(stackTrace);
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
		ulong value = 0uL;
		if (!ReadPointer(num2, out value))
		{
			return list;
		}
		num2 += (ulong)(IntPtr.Size * 2);
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
			list.Add(new DesktopStackFrame(this, value2, value3, value4));
			num2 += (ulong)num;
		}
		return list;
	}

	internal override IThreadStoreData GetThreadStoreData()
	{
		if (_sos.GetThreadStoreData(out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override string GetAppBase(ulong appDomain)
	{
		if (_sos.GetApplicationBase(appDomain, 0, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder((int)pNeeded);
		if (_sos.GetApplicationBase(appDomain, (int)pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override string GetConfigFile(ulong appDomain)
	{
		if (_sos.GetAppDomainConfigFile(appDomain, 0, null, out var pNeeded) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder((int)pNeeded);
		if (_sos.GetAppDomainConfigFile(appDomain, (int)pNeeded, stringBuilder, out pNeeded) < 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	internal override IMethodDescData GetMDForIP(ulong ip)
	{
		if (_sos.GetMethodDescPtrFromIP(ip, out var ppMD) < 0 || ppMD == 0L)
		{
			if (_sos.GetCodeHeaderData(ip, out var data) < 0)
			{
				return null;
			}
			if ((ppMD = data.MethodDescPtr) == 0L)
			{
				return null;
			}
		}
		V45MethodDescDataWrapper v45MethodDescDataWrapper = new V45MethodDescDataWrapper();
		if (!v45MethodDescDataWrapper.Init(_sos, ppMD))
		{
			return null;
		}
		return v45MethodDescDataWrapper;
	}

	protected override ulong GetThreadFromThinlock(uint threadId)
	{
		if (_sos.GetThreadFromThinlockID(threadId, out var pThread) < 0)
		{
			return 0uL;
		}
		return pThread;
	}

	internal override int GetSyncblkCount()
	{
		if (_sos.GetSyncBlockData(1u, out var data) < 0)
		{
			return 0;
		}
		return (int)data.TotalCount;
	}

	internal override ISyncBlkData GetSyncblkData(int index)
	{
		if (_sos.GetSyncBlockData((uint)(index + 1), out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override IThreadPoolData GetThreadPoolData()
	{
		if (_sos.GetThreadpoolData(out var data) < 0)
		{
			return null;
		}
		return data;
	}

	internal override uint GetTlsSlot()
	{
		uint pIndex = 0u;
		if (_sos.GetTLSIndex(out pIndex) < 0)
		{
			return uint.MaxValue;
		}
		return pIndex;
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
		if (_sos.GetThreadpoolData(out var data) == 0)
		{
			ulong num = data.FirstWorkRequest;
			V45WorkRequestData requestData;
			while (num != 0L && _sos.GetWorkRequestData(num, out requestData) == 0)
			{
				yield return new DesktopNativeWorkItem(requestData);
				num = requestData.NextWorkRequest;
			}
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
}
