using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("2900B785-981D-4A6F-82DC-AE7B9DA08EA2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDRuntime
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsServerGC(out int pServerGC);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHeapCount(out int pHeapCount);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int ReadVirtual(ulong addr, [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] buffer, int requested, out int pRead);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int ReadPtr(ulong addr, out ulong pValue);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void Flush();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHeap([MarshalAs(UnmanagedType.Interface)] out IMDHeap ppHeap);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateAppDomains([MarshalAs(UnmanagedType.Interface)] out IMDAppDomainEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out IMDThreadEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateFinalizerQueue([MarshalAs(UnmanagedType.Interface)] out IMDObjectEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateGCHandles([MarshalAs(UnmanagedType.Interface)] out IMDHandleEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateMemoryRegions([MarshalAs(UnmanagedType.Interface)] out IMDMemoryRegionEnum ppEnum);
}
