using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("8E5310DF-0F3A-4456-ACC4-52DEE8754767")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDMemoryRegion
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRegionInfo(out ulong pAddress, out ulong pSize, out MDMemoryRegionType pType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out IMDAppDomain ppDomain);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetModule([MarshalAs(UnmanagedType.BStr)] out string pModule);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHeapNumber(out int pHeap);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetDisplayString([MarshalAs(UnmanagedType.BStr)] out string pName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetSegmentType(out MDSegmentType pType);
}
