using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct ISOSDacVTable
{
	public readonly IntPtr GetThreadStoreData;

	public readonly IntPtr GetAppDomainStoreData;

	public readonly IntPtr GetAppDomainList;

	public readonly IntPtr GetAppDomainData;

	public readonly IntPtr GetAppDomainName;

	public readonly IntPtr GetDomainFromContext;

	public readonly IntPtr GetAssemblyList;

	public readonly IntPtr GetAssemblyData;

	public readonly IntPtr GetAssemblyName;

	public readonly IntPtr GetMetaDataImport;

	public readonly IntPtr GetModuleData;

	public readonly IntPtr TraverseModuleMap;

	public readonly IntPtr GetAssemblyModuleList;

	public readonly IntPtr GetILForModule;

	public readonly IntPtr GetThreadData;

	public readonly IntPtr GetThreadFromThinlockID;

	public readonly IntPtr GetStackLimits;

	public readonly IntPtr GetMethodDescData;

	public readonly IntPtr GetMethodDescPtrFromIP;

	public readonly IntPtr GetMethodDescName;

	public readonly IntPtr GetMethodDescPtrFromFrame;

	public readonly IntPtr GetMethodDescFromToken;

	private readonly IntPtr GetMethodDescTransparencyData;

	public readonly IntPtr GetCodeHeaderData;

	public readonly IntPtr GetJitManagerList;

	public readonly IntPtr GetJitHelperFunctionName;

	private readonly IntPtr GetJumpThunkTarget;

	public readonly IntPtr GetThreadpoolData;

	public readonly IntPtr GetWorkRequestData;

	private readonly IntPtr GetHillClimbingLogEntry;

	public readonly IntPtr GetObjectData;

	public readonly IntPtr GetObjectStringData;

	public readonly IntPtr GetObjectClassName;

	public readonly IntPtr GetMethodTableName;

	public readonly IntPtr GetMethodTableData;

	public readonly IntPtr GetMethodTableSlot;

	public readonly IntPtr GetMethodTableFieldData;

	private readonly IntPtr GetMethodTableTransparencyData;

	public readonly IntPtr GetMethodTableForEEClass;

	public readonly IntPtr GetFieldDescData;

	public readonly IntPtr GetFrameName;

	public readonly IntPtr GetPEFileBase;

	public readonly IntPtr GetPEFileName;

	public readonly IntPtr GetGCHeapData;

	public readonly IntPtr GetGCHeapList;

	public readonly IntPtr GetGCHeapDetails;

	public readonly IntPtr GetGCHeapStaticData;

	public readonly IntPtr GetHeapSegmentData;

	private readonly IntPtr GetOOMData;

	private readonly IntPtr GetOOMStaticData;

	private readonly IntPtr GetHeapAnalyzeData;

	private readonly IntPtr GetHeapAnalyzeStaticData;

	private readonly IntPtr GetDomainLocalModuleData;

	public readonly IntPtr GetDomainLocalModuleDataFromAppDomain;

	public readonly IntPtr GetDomainLocalModuleDataFromModule;

	public readonly IntPtr GetThreadLocalModuleData;

	public readonly IntPtr GetSyncBlockData;

	private readonly IntPtr GetSyncBlockCleanupData;

	public readonly IntPtr GetHandleEnum;

	private readonly IntPtr GetHandleEnumForTypes;

	private readonly IntPtr GetHandleEnumForGC;

	private readonly IntPtr TraverseEHInfo;

	private readonly IntPtr GetNestedExceptionData;

	public readonly IntPtr GetStressLogAddress;

	public readonly IntPtr TraverseLoaderHeap;

	public readonly IntPtr GetCodeHeapList;

	public readonly IntPtr TraverseVirtCallStubHeap;

	public readonly IntPtr GetUsefulGlobals;

	public readonly IntPtr GetClrWatsonBuckets;

	public readonly IntPtr GetTLSIndex;

	public readonly IntPtr GetDacModuleHandle;

	public readonly IntPtr GetRCWData;

	public readonly IntPtr GetRCWInterfaces;

	public readonly IntPtr GetCCWData;

	public readonly IntPtr GetCCWInterfaces;

	private readonly IntPtr TraverseRCWCleanupList;

	public readonly IntPtr GetStackReferences;

	public readonly IntPtr GetRegisterName;

	public readonly IntPtr GetThreadAllocData;

	public readonly IntPtr GetHeapAllocData;

	public readonly IntPtr GetFailedAssemblyList;

	public readonly IntPtr GetPrivateBinPaths;

	public readonly IntPtr GetAssemblyLocation;

	public readonly IntPtr GetAppDomainConfigFile;

	public readonly IntPtr GetApplicationBase;

	public readonly IntPtr GetFailedAssemblyData;

	public readonly IntPtr GetFailedAssemblyLocation;

	public readonly IntPtr GetFailedAssemblyDisplayName;
}
