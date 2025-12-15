using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[InterfaceType(1)]
[Guid("21e9d9c0-fcb8-11df-8cff-0800200c9a66")]
public interface ICorDebugProcess5
{
	void GetGCHeapInformation(out COR_HEAPINFO pHeapInfo);

	void EnumerateHeap([MarshalAs(UnmanagedType.Interface)] out ICorDebugHeapEnum ppObjects);

	void EnumerateHeapRegions([MarshalAs(UnmanagedType.Interface)] out ICorDebugHeapSegmentEnum ppRegions);

	void GetObject([In] ulong addr, [MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectValue ppObject);

	void EnumerateGCReferences([In] int bEnumerateWeakReferences, [MarshalAs(UnmanagedType.Interface)] out ICorDebugGCReferenceEnum ppEnum);

	void EnumerateHandles([In] uint types, [MarshalAs(UnmanagedType.Interface)] out ICorDebugGCReferenceEnum ppEnum);

	void GetTypeID([In] ulong objAddr, out COR_TYPEID pId);

	void GetTypeForTypeID([In] COR_TYPEID id, out ICorDebugType type);

	void GetArrayLayout([In] COR_TYPEID id, out COR_ARRAY_LAYOUT layout);

	void GetTypeLayout([In] COR_TYPEID id, out COR_TYPE_LAYOUT layout);

	void GetTypeFields([In] COR_TYPEID id, int celt, [Out][MarshalAs(UnmanagedType.LPArray)] COR_FIELD[] fields, out int pceltNeeded);

	void EnableNGENPolicy(CorDebugNGENPolicyFlags ePolicy);
}
