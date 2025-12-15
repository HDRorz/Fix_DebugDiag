using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("5A73920A-F8C5-4FCB-A725-8564F41BB055")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDCCW
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetIUnknown(out ulong pIUnk);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetObject(out ulong pObject);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHandle(out ulong pHandle);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRefCount(out int pRefCnt);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateInterfaces([MarshalAs(UnmanagedType.Interface)] out IMDCOMInterfaceEnum ppEnum);
}
