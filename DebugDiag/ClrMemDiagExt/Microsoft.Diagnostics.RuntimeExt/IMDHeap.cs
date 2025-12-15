using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("B53137DF-FC18-4470-A0D9-8EE4F829C970")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDHeap
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetObjectType(ulong addr, [MarshalAs(UnmanagedType.Interface)] out IMDType ppType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetExceptionObject(ulong addr, [MarshalAs(UnmanagedType.Interface)] out IMDException ppExcep);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateRoots([MarshalAs(UnmanagedType.Interface)] out IMDRootEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateSegments([MarshalAs(UnmanagedType.Interface)] out IMDSegmentEnum ppEnum);
}
