using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("41CBFB96-45E4-4F2C-9002-82FD2ECD585F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDTarget
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRuntimeCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRuntimeInfo(int num, [MarshalAs(UnmanagedType.Interface)] out IMDRuntimeInfo ppInfo);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetPointerSize(out int pPointerSize);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void CreateRuntimeFromDac([MarshalAs(UnmanagedType.BStr)] string dacLocation, [MarshalAs(UnmanagedType.Interface)] out IMDRuntime ppRuntime);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void CreateRuntimeFromIXCLR([MarshalAs(UnmanagedType.IUnknown)] object ixCLRProcess, [MarshalAs(UnmanagedType.Interface)] out IMDRuntime ppRuntime);
}
