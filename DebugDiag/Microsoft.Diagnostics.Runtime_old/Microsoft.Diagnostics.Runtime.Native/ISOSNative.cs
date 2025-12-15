using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("90456375-3774-4c70-999a-a6fa78aab107")]
internal interface ISOSNative
{
	[PreserveSig]
	int Flush();

	[PreserveSig]
	int GetThreadStoreData(out NativeThreadStoreData pData);

	[PreserveSig]
	int GetThreadAddress(ulong teb, out ulong pThread);

	[PreserveSig]
	int GetThreadData(ulong addr, out NativeThreadData pThread);

	[PreserveSig]
	int GetCurrentExceptionObject(ulong thread, out ulong pExceptionRefAddress);

	[PreserveSig]
	int GetObjectData(ulong addr, out NativeObjectData pData);

	[PreserveSig]
	int GetEETypeData(ulong addr, out NativeMethodTableData pData);

	[PreserveSig]
	int GetGcHeapAnalyzeData_do_not_use();

	[PreserveSig]
	int GetGCHeapData(out LegacyGCInfo pData);

	[PreserveSig]
	int GetGCHeapList(int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ulong[] heaps, out int pNeeded);

	[PreserveSig]
	int GetGCHeapDetails(ulong heap, out NativeHeapDetails details);

	[PreserveSig]
	int GetGCHeapStaticData(out NativeHeapDetails data);

	[PreserveSig]
	int GetGCHeapSegment(ulong segment, out NativeSegementData pSegmentData);

	[PreserveSig]
	int GetFreeEEType(out ulong freeType);

	[PreserveSig]
	int DumpGCInfo_do_not_use(ulong codeAddr, IntPtr callback);

	[PreserveSig]
	int DumpEHInfo_do_not_use(ulong ehInfo, IntPtr symbolResolver, IntPtr callback);

	[PreserveSig]
	int DumpStackObjects(ulong threadAddr, IntPtr pCallback, IntPtr token);

	[PreserveSig]
	int TraverseStackRoots(ulong threadAddr, IntPtr pInitialContext, int initialContextSize, IntPtr pCallback, IntPtr token);

	[PreserveSig]
	int TraverseStaticRoots(IntPtr pCallback);

	[PreserveSig]
	int TraverseHandleTable(IntPtr pCallback, IntPtr token);

	[PreserveSig]
	int TraverseHandleTableFiltered(IntPtr pCallback, IntPtr token, int type, int gen);

	[PreserveSig]
	int GetCodeHeaderData(ulong ip, out NativeCodeHeader pData);

	[PreserveSig]
	int GetModuleList(int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ulong[] modules, out int pNeeded);

	[PreserveSig]
	int GetStressLogAddress_do_not_use();

	[PreserveSig]
	int GetStressLogData_do_not_use();

	[PreserveSig]
	int EnumStressLogMessages_do_not_use();

	[PreserveSig]
	int EnumStressLogMemRanges_do_not_use();

	[PreserveSig]
	int UpdateDebugEventFilter_do_not_use();
}
