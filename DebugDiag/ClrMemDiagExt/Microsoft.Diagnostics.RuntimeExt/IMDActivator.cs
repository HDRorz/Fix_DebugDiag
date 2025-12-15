using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("5DC19835-504C-47AF-B96B-06AF1A737AE9")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDActivator
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void CreateFromCrashDump([MarshalAs(UnmanagedType.BStr)] string crashdump, [MarshalAs(UnmanagedType.Interface)] out IMDTarget ppTarget);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void CreateFromIDebugClient([MarshalAs(UnmanagedType.IUnknown)] object iDebugClient, [MarshalAs(UnmanagedType.Interface)] out IMDTarget ppTarget);
}
