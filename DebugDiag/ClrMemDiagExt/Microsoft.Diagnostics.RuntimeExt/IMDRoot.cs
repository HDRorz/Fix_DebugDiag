using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("CF7DD882-7CF9-4F13-91C3-3E102CEDA943")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDRoot
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRootInfo(out ulong pAddress, out ulong pObjRef, out MDRootType pType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetType([MarshalAs(UnmanagedType.Interface)] out IMDType ppType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetName([MarshalAs(UnmanagedType.BStr)] out string ppName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out IMDAppDomain ppDomain);
}
