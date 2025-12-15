using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("E630B609-502A-43F7-8F88-1598F21F2848")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDCOMInterfaceEnum
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void Reset();

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([MarshalAs(UnmanagedType.Interface)] IMDType pType, out ulong pInterfacePtr);
}
