using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("FDB00A49-0584-4C24-95A4-B5CB08917596")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDHandle
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHandleData(out ulong pAddr, out ulong pObjRef, out MDHandleTypes pType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsStrong(out int pStrong);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRefCount(out int pRefCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetDependentTarget(out ulong pTarget);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out IMDAppDomain ppDomain);
}
