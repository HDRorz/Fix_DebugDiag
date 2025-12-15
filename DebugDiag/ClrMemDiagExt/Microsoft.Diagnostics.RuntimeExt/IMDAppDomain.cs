using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("6558598B-772F-4686-8F14-72C565072171")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDAppDomain
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetName([MarshalAs(UnmanagedType.BStr)] out string pName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetID(out int pID);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetAddress(out ulong pAddress);
}
