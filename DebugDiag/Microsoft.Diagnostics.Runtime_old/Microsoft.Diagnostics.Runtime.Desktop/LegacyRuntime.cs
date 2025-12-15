#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class LegacyRuntime : DesktopRuntimeBase
{
	private byte[] _buffer = new byte[32768];

	private DesktopVersion _version;

	private int _minor;

	internal override DesktopVersion CLRVersion => _version;

	public LegacyRuntime(DataTargetImpl dt, DacLibrary lib, DesktopVersion version, int minor)
		: base(dt, lib)
	{
		_version = version;
		_minor = minor;
		if (!GetCommonMethodTables(ref _commonMTs))
		{
			throw new ClrDiagnosticsException("Could not request common MethodTable list.", ClrDiagnosticsException.HR.DacError);
		}
		byte[] array = new byte[4];
		if (!Request(3758096384u, null, array))
		{
			throw new ClrDiagnosticsException("Failed to request dac version.", ClrDiagnosticsException.HR.DacError);
		}
		if (BitConverter.ToInt32(array, 0) != 8)
		{
			throw new ClrDiagnosticsException("Unsupported dac version.", ClrDiagnosticsException.HR.DacError);
		}
	}

	protected override void InitApi()
	{
	}

	internal override ulong[] GetAssemblyList(ulong appDomain, int count)
	{
		return RequestAddrList(4026531848u, appDomain, count);
	}

	internal override ulong[] GetModuleList(ulong assembly, int count)
	{
		return RequestAddrList(4026531881u, assembly, count);
	}

	internal override IAssemblyData GetAssemblyData(ulong appDomain, ulong assembly)
	{
		if (assembly == 0L)
		{
			return null;
		}
		return Request<IAssemblyData, LegacyAssemblyData>(4026531850u, assembly);
	}

	public override IEnumerable<ClrHandle> EnumerateHandles()
	{
		HandleTableWalker handleTableWalker = new HandleTableWalker(this);
		byte[] array = null;
		array = ((CLRVersion != 0) ? handleTableWalker.V4Request : handleTableWalker.V2Request);
		if (!Request(4026531894u, array, null))
		{
			Trace.WriteLine("Warning, GetHandles() method failed, returning partial results.");
		}
		return handleTableWalker.Handles;
	}

	internal override bool TraverseHeap(ulong heap, LoaderHeapTraverse callback)
	{
		byte[] array = new byte[16];
		WriteValueToBuffer(heap, array, 0);
		WriteValueToBuffer(Marshal.GetFunctionPointerForDelegate(callback), array, 8);
		return Request(4026531901u, array, null);
	}

	internal override bool TraverseStubHeap(ulong appDomain, int type, LoaderHeapTraverse callback)
	{
		byte[] array = ((IntPtr.Size != 4) ? new byte[24] : new byte[16]);
		WriteValueToBuffer(appDomain, array, 0);
		WriteValueToBuffer(type, array, 8);
		WriteValueToBuffer(Marshal.GetFunctionPointerForDelegate(callback), array, 12);
		return Request(4026531906u, array, null);
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
		byte[] array = new byte[16];
		Buffer.BlockCopy(BitConverter.GetBytes(addr), 0, array, 0, 8);
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<IThreadData, V2ThreadData>(4026531857u, array);
		}
		return Request<IThreadData, V4ThreadData>(4026531857u, array);
	}

	internal override IHeapDetails GetSvrHeapDetails(ulong addr)
	{
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<IHeapDetails, V2HeapDetails>(4026531884u, addr);
		}
		return Request<IHeapDetails, V4HeapDetails>(4026531884u, addr);
	}

	internal override IHeapDetails GetWksHeapDetails()
	{
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<IHeapDetails, V2HeapDetails>(4026531885u);
		}
		return Request<IHeapDetails, V4HeapDetails>(4026531885u);
	}

	internal override ulong[] GetServerHeapList()
	{
		return RequestAddrList(4026531883u, base.HeapCount);
	}

	internal override IList<ulong> GetAppDomainList(int count)
	{
		return RequestAddrList(4026531842u, count);
	}

	internal override IMethodTableData GetMethodTableData(ulong addr)
	{
		return Request<IMethodTableData, LegacyMethodTableData>(4026531872u, addr);
	}

	internal override IGCInfo GetGCInfo()
	{
		return Request<IGCInfo, LegacyGCInfo>(4026531882u);
	}

	internal override ISegmentData GetSegmentData(ulong segmentAddr)
	{
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<ISegmentData, V2SegmentData>(4026531886u, segmentAddr);
		}
		return Request<ISegmentData, V4SegmentData>(4026531886u, segmentAddr);
	}

	internal override string GetAppDomaminName(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		ClearBuffer();
		if (!Request(4026531844u, addr, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	private void ClearBuffer()
	{
		_buffer[0] = 0;
		_buffer[1] = 0;
	}

	internal override string GetAssemblyName(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		ClearBuffer();
		if (!Request(4026531851u, addr, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override IAppDomainStoreData GetAppDomainStoreData()
	{
		return Request<IAppDomainStoreData, LegacyAppDomainStoreData>(4026531841u);
	}

	internal override IAppDomainData GetAppDomainData(ulong addr)
	{
		return Request<IAppDomainData, LegacyAppDomainData>(4026531843u, addr);
	}

	internal override bool GetCommonMethodTables(ref CommonMethodTables mCommonMTs)
	{
		return RequestStruct(4026531908u, ref mCommonMTs);
	}

	internal override string GetNameForMT(ulong mt)
	{
		ClearBuffer();
		if (!Request(4026531871u, mt, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override string GetPEFileName(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		ClearBuffer();
		if (!Request(4026531880u, addr, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override IModuleData GetModuleData(ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<IModuleData, V2ModuleData>(4026531876u, addr);
		}
		return Request<IModuleData, V4ModuleData>(4026531876u, addr);
	}

	internal override ulong GetModuleForMT(ulong mt)
	{
		if (mt == 0L)
		{
			return 0uL;
		}
		IMethodTableData methodTableData = GetMethodTableData(mt);
		if (methodTableData == null)
		{
			return 0uL;
		}
		return ((CLRVersion != 0) ? Request<IEEClassData, V4EEClassData>(4026531873u, methodTableData.EEClass) : Request<IEEClassData, V2EEClassData>(4026531873u, methodTableData.EEClass))?.Module ?? 0;
	}

	internal override IEnumerable<ICodeHeap> EnumerateJitHeaps()
	{
		byte[] output = new byte[4];
		if (!Request(4026531898u, null, output))
		{
			yield break;
		}
		int JitManagerSize = Marshal.SizeOf(typeof(LegacyJitManagerInfo));
		int count = BitConverter.ToInt32(output, 0);
		int num = JitManagerSize * count;
		if (num <= 0)
		{
			yield break;
		}
		output = new byte[num];
		if (!Request(4026531902u, null, output))
		{
			yield break;
		}
		LegacyJitCodeHeapInfo legacyJitCodeHeapInfo = default(LegacyJitCodeHeapInfo);
		int CodeHeapTypeOffset = Marshal.OffsetOf(typeof(LegacyJitCodeHeapInfo), "codeHeapType").ToInt32();
		int AddressOffset = Marshal.OffsetOf(typeof(LegacyJitCodeHeapInfo), "address").ToInt32();
		int CurrAddrOffset = Marshal.OffsetOf(typeof(LegacyJitCodeHeapInfo), "currentAddr").ToInt32();
		int JitCodeHeapInfoSize = Marshal.SizeOf(typeof(LegacyJitCodeHeapInfo));
		int i = 0;
		while (i < count)
		{
			int num2;
			if ((BitConverter.ToInt32(output, i * JitManagerSize + 8) & 3) == 0)
			{
				ulong value = BitConverter.ToUInt64(output, i * JitManagerSize);
				byte[] array = new byte[16];
				WriteValueToBuffer(value, array, 0);
				if (Request(4026531903u, array, array))
				{
					int heapCount = BitConverter.ToInt32(array, 8);
					byte[] codeHeapBuffer = new byte[heapCount * JitCodeHeapInfoSize];
					if (Request(4026531904u, array, codeHeapBuffer))
					{
						for (int j = 0; j < heapCount; j = num2)
						{
							legacyJitCodeHeapInfo.address = BitConverter.ToUInt64(codeHeapBuffer, j * JitCodeHeapInfoSize + AddressOffset);
							legacyJitCodeHeapInfo.codeHeapType = BitConverter.ToUInt32(codeHeapBuffer, j * JitCodeHeapInfoSize + CodeHeapTypeOffset);
							legacyJitCodeHeapInfo.currentAddr = BitConverter.ToUInt64(codeHeapBuffer, j * JitCodeHeapInfoSize + CurrAddrOffset);
							yield return legacyJitCodeHeapInfo;
							num2 = j + 1;
						}
					}
				}
			}
			num2 = i + 1;
			i = num2;
		}
	}

	internal override IFieldInfo GetFieldInfo(ulong mt)
	{
		IMethodTableData methodTableData = GetMethodTableData(mt);
		if (CLRVersion == DesktopVersion.v2)
		{
			return Request<IFieldInfo, V2EEClassData>(4026531873u, methodTableData.EEClass);
		}
		return Request<IFieldInfo, V4EEClassData>(4026531873u, methodTableData.EEClass);
	}

	internal override IFieldData GetFieldData(ulong fieldDesc)
	{
		return Request<IFieldData, LegacyFieldData>(4026531874u, fieldDesc);
	}

	internal override IObjectData GetObjectData(ulong objRef)
	{
		return Request<IObjectData, LegacyObjectData>(4026531867u, objRef);
	}

	internal override IMetadata GetMetadataImport(ulong module)
	{
		IModuleData moduleData = GetModuleData(module);
		if (moduleData != null && moduleData.LegacyMetaDataImport != null)
		{
			return moduleData.LegacyMetaDataImport as IMetadata;
		}
		return null;
	}

	internal override ICCWData GetCCWData(ulong ccw)
	{
		return null;
	}

	internal override IRCWData GetRCWData(ulong rcw)
	{
		return null;
	}

	internal override COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count)
	{
		return null;
	}

	internal override COMInterfacePointerData[] GetRCWInterfaces(ulong rcw, int count)
	{
		return null;
	}

	internal override IDomainLocalModuleData GetDomainLocalModule(ulong appDomain, ulong id)
	{
		byte[] byteArrayForStruct = GetByteArrayForStruct<LegacyDomainLocalModuleData>();
		int offset = WriteValueToBuffer(appDomain, byteArrayForStruct, 0);
		offset = WriteValueToBuffer(new IntPtr((long)id), byteArrayForStruct, offset);
		if (Request(4026531890u, null, byteArrayForStruct))
		{
			return ConvertStruct<IDomainLocalModuleData, LegacyDomainLocalModuleData>(byteArrayForStruct);
		}
		return null;
	}

	internal override IList<ulong> GetMethodTableList(ulong module)
	{
		List<ulong> mts = new List<ulong>();
		ModuleMapTraverse moduleMapTraverse = delegate(uint index, ulong mt, IntPtr token)
		{
			mts.Add(mt);
		};
		LegacyModuleMapTraverseArgs legacyModuleMapTraverseArgs = new LegacyModuleMapTraverseArgs
		{
			pCallback = Marshal.GetFunctionPointerForDelegate(moduleMapTraverse),
			module = module
		};
		byte[] byteArrayForStruct = GetByteArrayForStruct<LegacyModuleMapTraverseArgs>();
		IntPtr intPtr = Marshal.AllocHGlobal(byteArrayForStruct.Length);
		Marshal.StructureToPtr(legacyModuleMapTraverseArgs, intPtr, fDeleteOld: true);
		Marshal.Copy(intPtr, byteArrayForStruct, 0, byteArrayForStruct.Length);
		Marshal.FreeHGlobal(intPtr);
		Request(4026531877u, byteArrayForStruct, null);
		GC.KeepAlive(moduleMapTraverse);
		return mts;
	}

	internal override IDomainLocalModuleData GetDomainLocalModule(ulong module)
	{
		return Request<IDomainLocalModuleData, LegacyDomainLocalModuleData>(4026531891u, module);
	}

	private ulong GetMethodDescFromIp(ulong ip)
	{
		if (ip == 0L)
		{
			return 0uL;
		}
		IMethodDescData methodDescData = Request<IMethodDescData, V35MethodDescData>(4026531861u, ip);
		if (methodDescData == null)
		{
			methodDescData = Request<IMethodDescData, V2MethodDescData>(4026531861u, ip);
		}
		if (methodDescData == null)
		{
			CodeHeaderData t = default(CodeHeaderData);
			if (RequestStruct(4026531864u, ip, ref t))
			{
				return t.MethodDescPtr;
			}
		}
		return methodDescData?.MethodDesc ?? 0;
	}

	internal override string GetNameForMD(ulong md)
	{
		ClearBuffer();
		if (!Request(4026531862u, md, _buffer))
		{
			return "<Error>";
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override uint GetMetadataToken(ulong mt)
	{
		uint result = uint.MaxValue;
		IMethodTableData methodTableData = GetMethodTableData(mt);
		if (methodTableData != null)
		{
			byte[] array = null;
			array = ((CLRVersion != 0) ? GetByteArrayForStruct<V4EEClassData>() : GetByteArrayForStruct<V2EEClassData>());
			if (Request(4026531873u, methodTableData.EEClass, array))
			{
				if (CLRVersion == DesktopVersion.v2)
				{
					GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
					V2EEClassData obj = (V2EEClassData)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(V2EEClassData));
					gCHandle.Free();
					result = obj.token;
				}
				else
				{
					GCHandle gCHandle2 = GCHandle.Alloc(array, GCHandleType.Pinned);
					V4EEClassData obj2 = (V4EEClassData)Marshal.PtrToStructure(gCHandle2.AddrOfPinnedObject(), typeof(V4EEClassData));
					gCHandle2.Free();
					result = obj2.token;
				}
			}
		}
		return result;
	}

	protected override DesktopStackFrame GetStackFrame(int res, ulong ip, ulong sp, ulong frameVtbl)
	{
		ClearBuffer();
		if (res >= 0 && frameVtbl != 0L)
		{
			ClrMethod innerMethod = null;
			string method = "Unknown Frame";
			if (Request(4026531868u, frameVtbl, _buffer))
			{
				method = RuntimeBase.BytesToString(_buffer);
			}
			IMethodDescData methodDescData = GetMethodDescData(4026531863u, sp);
			if (methodDescData != null)
			{
				innerMethod = DesktopMethod.Create(this, methodDescData);
			}
			return new DesktopStackFrame(this, sp, method, innerMethod);
		}
		ulong methodDescFromIp = GetMethodDescFromIp(ip);
		return new DesktopStackFrame(this, ip, sp, methodDescFromIp);
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
		if (!GetStackTraceFromField(type, obj, out var stackTrace) && !ReadPointer(obj + GetStackTraceOffset(), out stackTrace))
		{
			return list;
		}
		if (stackTrace == 0L)
		{
			return list;
		}
		DesktopGCHeap desktopGCHeap = TryGetHeap();
		ClrType clrType = desktopGCHeap.GetObjectType(stackTrace);
		if (clrType == null)
		{
			clrType = desktopGCHeap.ArrayType;
		}
		if (!clrType.IsArray)
		{
			return list;
		}
		if (clrType.GetArrayLength(stackTrace) == 0)
		{
			return list;
		}
		int num = ((CLRVersion == DesktopVersion.v2) ? (IntPtr.Size * 4) : (IntPtr.Size * 3));
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

	internal override IMethodDescData GetMethodDescData(ulong md)
	{
		return GetMethodDescData(4026531860u, md);
	}

	internal override IList<ulong> GetMethodDescList(ulong methodTable)
	{
		IMethodTableData methodTableData = Request<IMethodTableData, LegacyMethodTableData>(4026531872u, methodTable);
		ulong[] array = new ulong[methodTableData.NumMethods];
		if (methodTableData.NumMethods == 0)
		{
			return array;
		}
		CodeHeaderData t = default(CodeHeaderData);
		byte[] array2 = new byte[16];
		byte[] array3 = new byte[8];
		WriteValueToBuffer(methodTable, array2, 0);
		for (int i = 0; i < methodTableData.NumMethods; i++)
		{
			WriteValueToBuffer(i, array2, 8);
			if (Request(4026531905u, array2, array3))
			{
				ulong addr = BitConverter.ToUInt64(array3, 0);
				if (RequestStruct(4026531864u, addr, ref t))
				{
					array[i] = t.MethodDescPtr;
				}
			}
		}
		return array;
	}

	internal override ulong GetThreadStaticPointer(ulong thread, ClrElementType type, uint offset, uint moduleId, bool shared)
	{
		return 0uL;
	}

	internal override IThreadStoreData GetThreadStoreData()
	{
		LegacyThreadStoreData t = default(LegacyThreadStoreData);
		if (!RequestStruct(4026531840u, ref t))
		{
			return null;
		}
		return t;
	}

	internal override string GetAppBase(ulong appDomain)
	{
		ClearBuffer();
		if (!Request(4026531845u, appDomain, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override string GetConfigFile(ulong appDomain)
	{
		ClearBuffer();
		if (!Request(4026531847u, appDomain, _buffer))
		{
			return null;
		}
		return RuntimeBase.BytesToString(_buffer);
	}

	internal override IMethodDescData GetMDForIP(ulong ip)
	{
		if (GetMethodDescData(4026531861u, ip) == null && GetMethodDescFromIp(ip) != 0L)
		{
			return GetMethodDescData(4026531860u, ip);
		}
		return null;
	}

	internal override IEnumerable<NativeWorkItem> EnumerateWorkItems()
	{
		IThreadPoolData threadPoolData = GetThreadPoolData();
		if (_version != 0)
		{
			yield break;
		}
		ulong num = threadPoolData.FirstWorkRequest;
		byte[] bytes = GetByteArrayForStruct<DacpWorkRequestData>();
		while (Request(4026531866u, num, bytes))
		{
			GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			DacpWorkRequestData result = (DacpWorkRequestData)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(DacpWorkRequestData));
			gCHandle.Free();
			yield return new DesktopNativeWorkItem(result);
			num = result.NextWorkRequest;
			if (num == 0L)
			{
				break;
			}
		}
	}

	private IMethodDescData GetMethodDescData(uint request_id, ulong addr)
	{
		if (addr == 0L)
		{
			return null;
		}
		IMethodDescData methodDescData;
		if (_version == DesktopVersion.v4 || _minor > 4016)
		{
			methodDescData = Request<IMethodDescData, V35MethodDescData>(request_id, addr);
		}
		else if (_minor < 3053)
		{
			methodDescData = Request<IMethodDescData, V2MethodDescData>(request_id, addr);
		}
		else
		{
			methodDescData = Request<IMethodDescData, V35MethodDescData>(request_id, addr);
			if (methodDescData == null)
			{
				methodDescData = Request<IMethodDescData, V2MethodDescData>(request_id, addr);
			}
		}
		if (methodDescData == null && request_id == 4026531861u)
		{
			CodeHeaderData t = default(CodeHeaderData);
			if (RequestStruct(4026531864u, addr, ref t))
			{
				methodDescData = GetMethodDescData(4026531860u, t.MethodDescPtr);
			}
		}
		return methodDescData;
	}

	protected override ulong GetThreadFromThinlock(uint threadId)
	{
		byte[] array = new byte[4];
		WriteValueToBuffer(threadId, array, 0);
		byte[] array2 = new byte[8];
		if (!Request(4026531858u, array, array2))
		{
			return 0uL;
		}
		return BitConverter.ToUInt64(array2, 0);
	}

	internal override int GetSyncblkCount()
	{
		return (int)(Request<ISyncBlkData, LegacySyncBlkData>(4026531892u, 1u)?.TotalCount ?? 0);
	}

	internal override ISyncBlkData GetSyncblkData(int index)
	{
		if (index < 0)
		{
			return null;
		}
		return Request<ISyncBlkData, LegacySyncBlkData>(4026531892u, (uint)(index + 1));
	}

	internal override IThreadPoolData GetThreadPoolData()
	{
		if (_version == DesktopVersion.v2)
		{
			return Request<IThreadPoolData, V2ThreadPoolData>(4026531865u);
		}
		return Request<IThreadPoolData, V4ThreadPoolData>(4026531921u);
	}

	internal override uint GetTlsSlot()
	{
		byte[] array = new byte[4];
		if (!Request(4026531909u, null, array))
		{
			return uint.MaxValue;
		}
		return BitConverter.ToUInt32(array, 0);
	}

	internal override uint GetThreadTypeIndex()
	{
		if (_version == DesktopVersion.v2)
		{
			if (PointerSize != 4)
			{
				return 13u;
			}
			return 12u;
		}
		return 11u;
	}

	protected override uint GetRWLockDataOffset()
	{
		if (PointerSize == 8)
		{
			return 56u;
		}
		return 36u;
	}

	internal override uint GetStringFirstCharOffset()
	{
		if (PointerSize == 8)
		{
			return 16u;
		}
		return 12u;
	}

	internal override uint GetStringLengthOffset()
	{
		if (PointerSize == 8)
		{
			return 12u;
		}
		return 8u;
	}

	internal override uint GetExceptionHROffset()
	{
		if (PointerSize != 8)
		{
			return 56u;
		}
		return 116u;
	}
}
