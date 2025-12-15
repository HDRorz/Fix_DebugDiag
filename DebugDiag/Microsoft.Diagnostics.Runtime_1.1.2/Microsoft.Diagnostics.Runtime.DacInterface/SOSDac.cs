using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class SOSDac : CallableCOMWrapper
{
	public enum ModuleMapTraverseKind
	{
		TypeDefToMethodTable,
		TypeRefToMethodTable
	}

	public delegate void ModuleMapTraverse(uint index, ulong methodTable, IntPtr token);

	public delegate void LoaderHeapTraverse(ulong address, IntPtr size, int isCurrent);

	private delegate int GetMethodDescFromTokenDelegate(IntPtr self, ulong module, uint token, out ulong methodDesc);

	private delegate int GetMethodDescDataDelegate(IntPtr self, ulong md, ulong ip, out MethodDescData data, int count, [Out] RejitData[] rejitData, out int needed);

	private delegate int DacGetIntPtr(IntPtr self, out IntPtr data);

	private delegate int DacGetUlongWithArg(IntPtr self, ulong arg, out ulong data);

	private delegate int DacGetUlongWithArgs(IntPtr self, ulong arg, uint id, out ulong data);

	private delegate int DacGetUInt(IntPtr self, out uint data);

	private delegate int DacGetIntPtrWithArg(IntPtr self, uint addr, out IntPtr data);

	private delegate int DacGetThreadData(IntPtr self, ulong addr, out ThreadData data);

	private delegate int DacGetHeapDetailsWithArg(IntPtr self, ulong addr, out HeapDetails data);

	private delegate int DacGetHeapDetails(IntPtr self, out HeapDetails data);

	private delegate int DacGetUlongArray(IntPtr self, int count, [Out] ulong[] values, out int needed);

	private delegate int DacGetUlongArrayWithArg(IntPtr self, ulong arg, int count, [Out] ulong[] values, out int needed);

	private delegate int DacGetCharArrayWithArg(IntPtr self, ulong arg, int count, [Out] byte[] values, out int needed);

	private delegate int DacGetByteArrayWithArg(IntPtr self, ulong arg, int count, [Out] byte[] values, out int needed);

	private delegate int DacGetAssemblyData(IntPtr self, ulong in1, ulong in2, out AssemblyData data);

	private delegate int DacGetADStoreData(IntPtr self, out AppDomainStoreData data);

	private delegate int DacGetGCInfoData(IntPtr self, out GCInfo data);

	private delegate int DacGetCommonMethodTables(IntPtr self, out CommonMethodTables data);

	private delegate int DacGetThreadPoolData(IntPtr self, out ThreadPoolData data);

	private delegate int DacGetThreadStoreData(IntPtr self, out ThreadStoreData data);

	private delegate int DacGetMTData(IntPtr self, ulong addr, out MethodTableData data);

	private delegate int DacGetModuleData(IntPtr self, ulong addr, out ModuleData data);

	private delegate int DacGetSegmentData(IntPtr self, ulong addr, out SegmentData data);

	private delegate int DacGetAppDomainData(IntPtr self, ulong addr, out AppDomainData data);

	private delegate int DacGetJitManagerInfo(IntPtr self, ulong addr, out JitManagerInfo data);

	private delegate int DacGetSyncBlockData(IntPtr self, int index, out SyncBlockData data);

	private delegate int DacGetCodeHeaderData(IntPtr self, ulong addr, out CodeHeaderData data);

	private delegate int DacGetFieldInfo(IntPtr self, ulong addr, out V4FieldInfo data);

	private delegate int DacGetFieldData(IntPtr self, ulong addr, out FieldData data);

	private delegate int DacGetObjectData(IntPtr self, ulong addr, out V45ObjectData data);

	private delegate int DacGetCCWData(IntPtr self, ulong addr, out CCWData data);

	private delegate int DacGetRCWData(IntPtr self, ulong addr, out RCWData data);

	private delegate int DacGetWorkRequestData(IntPtr self, ulong addr, out WorkRequestData data);

	private delegate int DacGetLocalModuleData(IntPtr self, ulong addr, out DomainLocalModuleData data);

	private delegate int DacGetThreadFromThinLock(IntPtr self, uint id, out ulong data);

	private delegate int DacGetCodeHeaps(IntPtr self, ulong addr, int count, [Out] JitCodeHeapInfo[] values, out int needed);

	private delegate int DacGetCOMPointers(IntPtr self, ulong addr, int count, [Out] COMInterfacePointerData[] values, out int needed);

	private delegate int DacGetDomainLocalModuleDataFromAppDomain(IntPtr self, ulong appDomainAddr, int moduleID, out DomainLocalModuleData data);

	private delegate int DacGetThreadLocalModuleData(IntPtr self, ulong addr, uint id, out ThreadLocalModuleData data);

	private delegate int DacTraverseLoaderHeap(IntPtr self, ulong addr, IntPtr callback);

	private delegate int DacTraverseStubHeap(IntPtr self, ulong addr, int type, IntPtr callback);

	private delegate int DacTraverseModuleMap(IntPtr self, int type, ulong addr, IntPtr callback, IntPtr param);

	private delegate int DacGetJitManagers(IntPtr self, int count, [Out] JitManagerInfo[] jitManagers, out int pNeeded);

	private delegate int GetMetaDataImportDelegate(IntPtr self, ulong addr, out IntPtr iunk);

	internal static Guid IID_ISOSDac = new Guid("436f00f2-b42a-4b9f-870c-e73db66ae930");

	private static RejitData[] s_emptyRejit;

	private readonly DacLibrary _library;

	private const int CharBufferSize = 256;

	private byte[] _buffer = new byte[256];

	private DacGetIntPtr _getHandleEnum;

	private DacGetIntPtrWithArg _getStackRefEnum;

	private DacGetThreadData _getThreadData;

	private DacGetHeapDetailsWithArg _getGCHeapDetails;

	private DacGetHeapDetails _getGCHeapStaticData;

	private DacGetUlongArray _getGCHeapList;

	private DacGetUlongArray _getAppDomainList;

	private DacGetUlongArrayWithArg _getAssemblyList;

	private DacGetUlongArrayWithArg _getModuleList;

	private DacGetAssemblyData _getAssemblyData;

	private DacGetADStoreData _getAppDomainStoreData;

	private DacGetMTData _getMethodTableData;

	private DacGetUlongWithArg _getMTForEEClass;

	private DacGetGCInfoData _getGCHeapData;

	private DacGetCommonMethodTables _getCommonMethodTables;

	private DacGetCharArrayWithArg _getMethodTableName;

	private DacGetByteArrayWithArg _getJitHelperFunctionName;

	private DacGetCharArrayWithArg _getPEFileName;

	private DacGetCharArrayWithArg _getAppDomainName;

	private DacGetCharArrayWithArg _getAssemblyName;

	private DacGetCharArrayWithArg _getAppBase;

	private DacGetCharArrayWithArg _getConfigFile;

	private DacGetModuleData _getModuleData;

	private DacGetSegmentData _getSegmentData;

	private DacGetAppDomainData _getAppDomainData;

	private DacGetJitManagers _getJitManagers;

	private DacTraverseLoaderHeap _traverseLoaderHeap;

	private DacTraverseStubHeap _traverseStubHeap;

	private DacTraverseModuleMap _traverseModuleMap;

	private DacGetFieldInfo _getFieldInfo;

	private DacGetFieldData _getFieldData;

	private DacGetObjectData _getObjectData;

	private DacGetCCWData _getCCWData;

	private DacGetRCWData _getRCWData;

	private DacGetCharArrayWithArg _getFrameName;

	private DacGetUlongWithArg _getMethodDescPtrFromFrame;

	private DacGetUlongWithArg _getMethodDescPtrFromIP;

	private DacGetCodeHeaderData _getCodeHeaderData;

	private DacGetSyncBlockData _getSyncBlock;

	private DacGetThreadPoolData _getThreadPoolData;

	private DacGetWorkRequestData _getWorkRequestData;

	private DacGetDomainLocalModuleDataFromAppDomain _getDomainLocalModuleDataFromAppDomain;

	private DacGetLocalModuleData _getDomainLocalModuleDataFromModule;

	private DacGetCodeHeaps _getCodeHeaps;

	private DacGetCOMPointers _getCCWInterfaces;

	private DacGetCOMPointers _getRCWInterfaces;

	private DacGetUlongWithArgs _getILForModule;

	private DacGetThreadLocalModuleData _getThreadLocalModuleData;

	private DacGetUlongWithArgs _getMethodTableSlot;

	private DacGetCharArrayWithArg _getMethodDescName;

	private DacGetThreadFromThinLock _getThreadFromThinlockId;

	private DacGetUInt _getTlsIndex;

	private DacGetThreadStoreData _getThreadStoreData;

	private GetMethodDescDataDelegate _getMethodDescData;

	private GetMetaDataImportDelegate _getMetaData;

	private GetMethodDescFromTokenDelegate _getMethodDescFromToken;

	private unsafe ISOSDacVTable* VTable => (ISOSDacVTable*)base._vtable;

	public SOSDac(DacLibrary library, IntPtr ptr)
		: base(library.OwningLibrary, ref IID_ISOSDac, ptr)
	{
		_library = library;
	}

	public SOSDac(CallableCOMWrapper toClone)
		: base(toClone)
	{
	}

	public unsafe RejitData[] GetRejitData(ulong md, ulong ip = 0uL)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodDescData, VTable->GetMethodDescData);
		if (CallableCOMWrapper.SUCCEEDED(_getMethodDescData(base.Self, md, ip, out var data, 0, null, out var needed)) && needed > 1)
		{
			RejitData[] array = new RejitData[needed];
			if (CallableCOMWrapper.SUCCEEDED(_getMethodDescData(base.Self, md, ip, out data, array.Length, array, out needed)))
			{
				return array;
			}
		}
		if (s_emptyRejit == null)
		{
			s_emptyRejit = new RejitData[0];
		}
		return s_emptyRejit;
	}

	public unsafe bool GetMethodDescData(ulong md, ulong ip, out MethodDescData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodDescData, VTable->GetMethodDescData);
		int needed;
		return CallableCOMWrapper.SUCCEEDED(_getMethodDescData(base.Self, md, ip, out data, 0, null, out needed));
	}

	public unsafe bool GetThreadStoreData(out ThreadStoreData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getThreadStoreData, VTable->GetThreadStoreData);
		return _getThreadStoreData(base.Self, out data) == 0;
	}

	public unsafe uint GetTlsIndex()
	{
		CallableCOMWrapper.InitDelegate(ref _getTlsIndex, VTable->GetTLSIndex);
		if (_getTlsIndex(base.Self, out var data) == 0)
		{
			return data;
		}
		return uint.MaxValue;
	}

	public unsafe ulong GetThreadFromThinlockId(uint id)
	{
		CallableCOMWrapper.InitDelegate(ref _getThreadFromThinlockId, VTable->GetThreadFromThinlockID);
		if (_getThreadFromThinlockId(base.Self, id, out var data) == 0)
		{
			return data;
		}
		return 0uL;
	}

	public unsafe string GetMethodDescName(ulong md)
	{
		if (md == 0L)
		{
			return null;
		}
		CallableCOMWrapper.InitDelegate(ref _getMethodDescName, VTable->GetMethodDescName);
		if (_getMethodDescName(base.Self, md, 0, null, out var needed) < 0)
		{
			return null;
		}
		byte[] array = AcquireBuffer(needed * 2);
		if (_getMethodDescName(base.Self, md, needed, array, out var needed2) < 0)
		{
			return null;
		}
		if (needed != needed2)
		{
			ReleaseBuffer(array);
			array = AcquireBuffer(needed2 * 2);
			if (_getMethodDescName(base.Self, md, needed2, array, out needed2) < 0)
			{
				return null;
			}
		}
		ReleaseBuffer(array);
		return string.Intern(Encoding.Unicode.GetString(array, 0, (needed2 - 1) * 2));
	}

	public unsafe ulong GetMethodTableSlot(ulong mt, int slot)
	{
		if (mt == 0L)
		{
			return 0uL;
		}
		CallableCOMWrapper.InitDelegate(ref _getMethodTableSlot, VTable->GetMethodTableSlot);
		if (_getMethodTableSlot(base.Self, mt, (uint)slot, out var data) == 0)
		{
			return data;
		}
		return 0uL;
	}

	public unsafe bool GetThreadLocalModuleData(ulong thread, uint index, out ThreadLocalModuleData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getThreadLocalModuleData, VTable->GetThreadLocalModuleData);
		return _getThreadLocalModuleData(base.Self, thread, index, out data) == 0;
	}

	public unsafe ulong GetILForModule(ulong moduleAddr, uint rva)
	{
		CallableCOMWrapper.InitDelegate(ref _getILForModule, VTable->GetILForModule);
		if (_getILForModule(base.Self, moduleAddr, rva, out var data) != 0)
		{
			return 0uL;
		}
		return data;
	}

	public unsafe COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count)
	{
		CallableCOMWrapper.InitDelegate(ref _getCCWInterfaces, VTable->GetCCWInterfaces);
		COMInterfacePointerData[] array = new COMInterfacePointerData[count];
		if (_getCCWInterfaces(base.Self, ccw, count, array, out var _) >= 0)
		{
			return array;
		}
		return null;
	}

	public unsafe COMInterfacePointerData[] GetRCWInterfaces(ulong ccw, int count)
	{
		CallableCOMWrapper.InitDelegate(ref _getRCWInterfaces, VTable->GetRCWInterfaces);
		COMInterfacePointerData[] array = new COMInterfacePointerData[count];
		if (_getRCWInterfaces(base.Self, ccw, count, array, out var _) >= 0)
		{
			return array;
		}
		return null;
	}

	public unsafe bool GetDomainLocalModuleDataFromModule(ulong module, out DomainLocalModuleData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getDomainLocalModuleDataFromModule, VTable->GetDomainLocalModuleDataFromModule);
		return CallableCOMWrapper.SUCCEEDED(_getDomainLocalModuleDataFromModule(base.Self, module, out data));
	}

	public unsafe bool GetDomainLocalModuleDataFromAppDomain(ulong appDomain, int id, out DomainLocalModuleData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getDomainLocalModuleDataFromAppDomain, VTable->GetDomainLocalModuleDataFromAppDomain);
		return CallableCOMWrapper.SUCCEEDED(_getDomainLocalModuleDataFromAppDomain(base.Self, appDomain, id, out data));
	}

	public unsafe bool GetWorkRequestData(ulong request, out WorkRequestData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getWorkRequestData, VTable->GetWorkRequestData);
		return CallableCOMWrapper.SUCCEEDED(_getWorkRequestData(base.Self, request, out data));
	}

	public unsafe bool GetThreadPoolData(out ThreadPoolData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getThreadPoolData, VTable->GetThreadpoolData);
		return CallableCOMWrapper.SUCCEEDED(_getThreadPoolData(base.Self, out data));
	}

	public unsafe bool GetSyncBlockData(int index, out SyncBlockData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getSyncBlock, VTable->GetSyncBlockData);
		return CallableCOMWrapper.SUCCEEDED(_getSyncBlock(base.Self, index, out data));
	}

	public unsafe string GetAppBase(ulong domain)
	{
		CallableCOMWrapper.InitDelegate(ref _getAppBase, VTable->GetApplicationBase);
		return GetString(_getAppBase, domain);
	}

	public unsafe string GetConfigFile(ulong domain)
	{
		CallableCOMWrapper.InitDelegate(ref _getConfigFile, VTable->GetAppDomainConfigFile);
		return GetString(_getConfigFile, domain);
	}

	public unsafe bool GetCodeHeaderData(ulong ip, out CodeHeaderData codeHeaderData)
	{
		if (ip == 0L)
		{
			codeHeaderData = default(CodeHeaderData);
			return false;
		}
		CallableCOMWrapper.InitDelegate(ref _getCodeHeaderData, VTable->GetCodeHeaderData);
		return _getCodeHeaderData(base.Self, ip, out codeHeaderData) == 0;
	}

	public unsafe ulong GetMethodDescPtrFromFrame(ulong frame)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodDescPtrFromFrame, VTable->GetMethodDescPtrFromFrame);
		if (_getMethodDescPtrFromFrame(base.Self, frame, out var data) == 0)
		{
			return data;
		}
		return 0uL;
	}

	public unsafe ulong GetMethodDescPtrFromIP(ulong frame)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodDescPtrFromIP, VTable->GetMethodDescPtrFromIP);
		if (_getMethodDescPtrFromIP(base.Self, frame, out var data) == 0)
		{
			return data;
		}
		return 0uL;
	}

	public unsafe string GetFrameName(ulong vtable)
	{
		CallableCOMWrapper.InitDelegate(ref _getFrameName, VTable->GetFrameName);
		return GetString(_getFrameName, vtable, skipNull: false) ?? "Unknown Frame";
	}

	public unsafe bool GetFieldInfo(ulong mt, out V4FieldInfo data)
	{
		CallableCOMWrapper.InitDelegate(ref _getFieldInfo, VTable->GetMethodTableFieldData);
		return CallableCOMWrapper.SUCCEEDED(_getFieldInfo(base.Self, mt, out data));
	}

	public unsafe bool GetFieldData(ulong fieldDesc, out FieldData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getFieldData, VTable->GetFieldDescData);
		return CallableCOMWrapper.SUCCEEDED(_getFieldData(base.Self, fieldDesc, out data));
	}

	public unsafe bool GetObjectData(ulong obj, out V45ObjectData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getObjectData, VTable->GetObjectData);
		return CallableCOMWrapper.SUCCEEDED(_getObjectData(base.Self, obj, out data));
	}

	public unsafe bool GetCCWData(ulong ccw, out CCWData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getCCWData, VTable->GetCCWData);
		return CallableCOMWrapper.SUCCEEDED(_getCCWData(base.Self, ccw, out data));
	}

	public unsafe bool GetRCWData(ulong rcw, out RCWData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getRCWData, VTable->GetRCWData);
		return CallableCOMWrapper.SUCCEEDED(_getRCWData(base.Self, rcw, out data));
	}

	public unsafe MetaDataImport GetMetadataImport(ulong module)
	{
		if (module == 0L)
		{
			return null;
		}
		CallableCOMWrapper.InitDelegate(ref _getMetaData, VTable->GetMetaDataImport);
		if (_getMetaData(base.Self, module, out var iunk) != 0)
		{
			return null;
		}
		try
		{
			return new MetaDataImport(_library, iunk);
		}
		catch (InvalidCastException)
		{
			return null;
		}
	}

	public unsafe bool GetCommonMethodTables(out CommonMethodTables commonMTs)
	{
		CallableCOMWrapper.InitDelegate(ref _getCommonMethodTables, VTable->GetUsefulGlobals);
		return _getCommonMethodTables(base.Self, out commonMTs) == 0;
	}

	public ulong[] GetAssemblyList(ulong appDomain)
	{
		return GetAssemblyList(appDomain, 0);
	}

	public unsafe ulong[] GetAssemblyList(ulong appDomain, int count)
	{
		return GetModuleOrAssembly(appDomain, count, ref _getAssemblyList, VTable->GetAssemblyList);
	}

	public unsafe bool GetAssemblyData(ulong domain, ulong assembly, out AssemblyData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getAssemblyData, VTable->GetAssemblyData);
		if (!CallableCOMWrapper.SUCCEEDED(_getAssemblyData(base.Self, domain, assembly, out data)))
		{
			return data.Address == assembly;
		}
		return true;
	}

	public unsafe bool GetAppDomainData(ulong addr, out AppDomainData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getAppDomainData, VTable->GetAppDomainData);
		if (!CallableCOMWrapper.SUCCEEDED(_getAppDomainData(base.Self, addr, out data)))
		{
			if (data.Address == addr)
			{
				return data.StubHeap != 0;
			}
			return false;
		}
		return true;
	}

	public unsafe string GetAppDomainName(ulong appDomain)
	{
		CallableCOMWrapper.InitDelegate(ref _getAppDomainName, VTable->GetAppDomainName);
		return GetString(_getAppDomainName, appDomain);
	}

	public unsafe string GetAssemblyName(ulong assembly)
	{
		CallableCOMWrapper.InitDelegate(ref _getAssemblyName, VTable->GetAssemblyName);
		return GetString(_getAssemblyName, assembly);
	}

	public unsafe bool GetAppDomainStoreData(out AppDomainStoreData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getAppDomainStoreData, VTable->GetAppDomainStoreData);
		return CallableCOMWrapper.SUCCEEDED(_getAppDomainStoreData(base.Self, out data));
	}

	public unsafe bool GetMethodTableData(ulong addr, out MethodTableData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodTableData, VTable->GetMethodTableData);
		return CallableCOMWrapper.SUCCEEDED(_getMethodTableData(base.Self, addr, out data));
	}

	public unsafe string GetMethodTableName(ulong mt)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodTableName, VTable->GetMethodTableName);
		return GetString(_getMethodTableName, mt);
	}

	public unsafe string GetJitHelperFunctionName(ulong addr)
	{
		CallableCOMWrapper.InitDelegate(ref _getJitHelperFunctionName, VTable->GetJitHelperFunctionName);
		return GetAsciiString(_getJitHelperFunctionName, addr);
	}

	public unsafe string GetPEFileName(ulong pefile)
	{
		CallableCOMWrapper.InitDelegate(ref _getPEFileName, VTable->GetPEFileName);
		return GetString(_getPEFileName, pefile);
	}

	private string GetString(DacGetCharArrayWithArg func, ulong addr, bool skipNull = true)
	{
		if (func(base.Self, addr, 0, null, out var needed) != 0)
		{
			return null;
		}
		if (needed == 0)
		{
			return "";
		}
		byte[] array = AcquireBuffer(needed * 2);
		if (func(base.Self, addr, needed, array, out needed) != 0)
		{
			ReleaseBuffer(array);
			return null;
		}
		if (skipNull)
		{
			needed--;
		}
		string @string = Encoding.Unicode.GetString(array, 0, needed * 2);
		ReleaseBuffer(array);
		return @string;
	}

	private string GetAsciiString(DacGetByteArrayWithArg func, ulong addr)
	{
		if (func(base.Self, addr, 0, null, out var needed) != 0)
		{
			return null;
		}
		if (needed == 0)
		{
			return "";
		}
		byte[] array = AcquireBuffer(needed);
		if (func(base.Self, addr, needed, array, out needed) != 0)
		{
			ReleaseBuffer(array);
			return null;
		}
		int num = Array.IndexOf(array, (byte)0);
		if (num >= 0)
		{
			needed = num;
		}
		string @string = Encoding.ASCII.GetString(array, 0, needed);
		ReleaseBuffer(array);
		return @string;
	}

	private byte[] AcquireBuffer(int size)
	{
		if (_buffer == null)
		{
			_buffer = new byte[256];
		}
		if (size > _buffer.Length)
		{
			return new byte[size];
		}
		byte[] buffer = _buffer;
		_buffer = null;
		return buffer;
	}

	private void ReleaseBuffer(byte[] buffer)
	{
		if (buffer.Length == 256)
		{
			_buffer = buffer;
		}
	}

	public unsafe ulong GetMethodTableByEEClass(ulong eeclass)
	{
		CallableCOMWrapper.InitDelegate(ref _getMTForEEClass, VTable->GetMethodTableForEEClass);
		if (_getMTForEEClass(base.Self, eeclass, out var data) == 0)
		{
			return data;
		}
		return 0uL;
	}

	public unsafe bool GetModuleData(ulong module, out ModuleData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getModuleData, VTable->GetModuleData);
		return CallableCOMWrapper.SUCCEEDED(_getModuleData(base.Self, module, out data));
	}

	public ulong[] GetModuleList(ulong assembly)
	{
		return GetModuleList(assembly, 0);
	}

	public unsafe ulong[] GetModuleList(ulong assembly, int count)
	{
		return GetModuleOrAssembly(assembly, count, ref _getModuleList, VTable->GetAssemblyModuleList);
	}

	private ulong[] GetModuleOrAssembly(ulong address, int count, ref DacGetUlongArrayWithArg func, IntPtr vtableEntry)
	{
		CallableCOMWrapper.InitDelegate(ref func, vtableEntry);
		int needed;
		if (count <= 0)
		{
			if (func(base.Self, address, 0, null, out needed) < 0)
			{
				return new ulong[0];
			}
			count = needed;
		}
		ulong[] array = new ulong[count];
		func(base.Self, address, array.Length, array, out needed);
		return array;
	}

	public unsafe ulong[] GetAppDomainList(int count = 0)
	{
		CallableCOMWrapper.InitDelegate(ref _getAppDomainList, VTable->GetAppDomainList);
		if (count <= 0)
		{
			if (!GetAppDomainStoreData(out var data))
			{
				return new ulong[0];
			}
			count = data.AppDomainCount;
		}
		ulong[] array = new ulong[count];
		if (_getAppDomainList(base.Self, array.Length, array, out var _) != 0)
		{
			return new ulong[0];
		}
		return array;
	}

	public unsafe bool GetThreadData(ulong address, out ThreadData data)
	{
		if (address == 0L)
		{
			data = default(ThreadData);
			return false;
		}
		CallableCOMWrapper.InitDelegate(ref _getThreadData, VTable->GetThreadData);
		int hresult = _getThreadData(base.Self, address, out data);
		if (IntPtr.Size == 4)
		{
			data = new ThreadData(ref data);
		}
		return CallableCOMWrapper.SUCCEEDED(hresult);
	}

	public unsafe bool GetGcHeapData(out GCInfo data)
	{
		CallableCOMWrapper.InitDelegate(ref _getGCHeapData, VTable->GetGCHeapData);
		return CallableCOMWrapper.SUCCEEDED(_getGCHeapData(base.Self, out data));
	}

	public unsafe bool GetSegmentData(ulong addr, out SegmentData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getSegmentData, VTable->GetHeapSegmentData);
		int num = _getSegmentData(base.Self, addr, out data);
		if (num == 0 && IntPtr.Size == 4)
		{
			data = new SegmentData(ref data);
		}
		return CallableCOMWrapper.SUCCEEDED(num);
	}

	public unsafe ulong[] GetHeapList(int heapCount)
	{
		CallableCOMWrapper.InitDelegate(ref _getGCHeapList, VTable->GetGCHeapList);
		ulong[] array = new ulong[heapCount];
		if (_getGCHeapList(base.Self, heapCount, array, out var _) != 0)
		{
			return null;
		}
		return array;
	}

	public unsafe bool GetServerHeapDetails(ulong addr, out HeapDetails data)
	{
		CallableCOMWrapper.InitDelegate(ref _getGCHeapDetails, VTable->GetGCHeapDetails);
		int hresult = _getGCHeapDetails(base.Self, addr, out data);
		if (IntPtr.Size == 4)
		{
			data = new HeapDetails(ref data);
		}
		return CallableCOMWrapper.SUCCEEDED(hresult);
	}

	public unsafe bool GetWksHeapDetails(out HeapDetails data)
	{
		CallableCOMWrapper.InitDelegate(ref _getGCHeapStaticData, VTable->GetGCHeapStaticData);
		int hresult = _getGCHeapStaticData(base.Self, out data);
		if (IntPtr.Size == 4)
		{
			data = new HeapDetails(ref data);
		}
		return CallableCOMWrapper.SUCCEEDED(hresult);
	}

	public unsafe JitManagerInfo[] GetJitManagers()
	{
		CallableCOMWrapper.InitDelegate(ref _getJitManagers, VTable->GetJitManagerList);
		if (_getJitManagers(base.Self, 0, null, out var pNeeded) != 0 || pNeeded == 0)
		{
			return new JitManagerInfo[0];
		}
		JitManagerInfo[] array = new JitManagerInfo[pNeeded];
		if (_getJitManagers(base.Self, array.Length, array, out pNeeded) != 0)
		{
			return new JitManagerInfo[0];
		}
		return array;
	}

	public unsafe JitCodeHeapInfo[] GetCodeHeapList(ulong jitManager)
	{
		CallableCOMWrapper.InitDelegate(ref _getCodeHeaps, VTable->GetCodeHeapList);
		if (_getCodeHeaps(base.Self, jitManager, 0, null, out var needed) != 0 || needed == 0)
		{
			return new JitCodeHeapInfo[0];
		}
		JitCodeHeapInfo[] array = new JitCodeHeapInfo[needed];
		if (_getCodeHeaps(base.Self, jitManager, array.Length, array, out needed) != 0)
		{
			return new JitCodeHeapInfo[0];
		}
		return array;
	}

	public unsafe bool TraverseModuleMap(ModuleMapTraverseKind mt, ulong module, ModuleMapTraverse traverse)
	{
		CallableCOMWrapper.InitDelegate(ref _traverseModuleMap, VTable->TraverseModuleMap);
		int num = _traverseModuleMap(base.Self, (int)mt, module, Marshal.GetFunctionPointerForDelegate((Delegate)traverse), IntPtr.Zero);
		GC.KeepAlive(traverse);
		return num == 0;
	}

	public unsafe bool TraverseLoaderHeap(ulong heap, LoaderHeapTraverse callback)
	{
		CallableCOMWrapper.InitDelegate(ref _traverseLoaderHeap, VTable->TraverseLoaderHeap);
		int num = _traverseLoaderHeap(base.Self, heap, Marshal.GetFunctionPointerForDelegate((Delegate)callback));
		GC.KeepAlive(callback);
		return num == 0;
	}

	public unsafe bool TraverseStubHeap(ulong heap, int type, LoaderHeapTraverse callback)
	{
		CallableCOMWrapper.InitDelegate(ref _traverseStubHeap, VTable->TraverseVirtCallStubHeap);
		int num = _traverseStubHeap(base.Self, heap, type, Marshal.GetFunctionPointerForDelegate((Delegate)callback));
		GC.KeepAlive(callback);
		return num == 0;
	}

	public unsafe SOSHandleEnum EnumerateHandles()
	{
		CallableCOMWrapper.InitDelegate(ref _getHandleEnum, VTable->GetHandleEnum);
		if (_getHandleEnum(base.Self, out var data) != 0)
		{
			return null;
		}
		return new SOSHandleEnum(_library, data);
	}

	public unsafe SOSStackRefEnum EnumerateStackRefs(uint osThreadId)
	{
		CallableCOMWrapper.InitDelegate(ref _getStackRefEnum, VTable->GetStackReferences);
		if (_getStackRefEnum(base.Self, osThreadId, out var data) != 0)
		{
			return null;
		}
		return new SOSStackRefEnum(_library, data);
	}

	public unsafe ulong GetMethodDescFromToken(ulong module, uint token)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodDescFromToken, VTable->GetMethodDescFromToken);
		if (_getMethodDescFromToken(base.Self, module, token, out var methodDesc) != 0)
		{
			return 0uL;
		}
		return methodDesc;
	}
}
