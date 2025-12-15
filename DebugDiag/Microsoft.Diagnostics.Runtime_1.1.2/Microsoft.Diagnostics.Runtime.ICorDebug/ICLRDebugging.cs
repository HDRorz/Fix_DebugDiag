using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D28F3C5A-9634-4206-A509-477552EEFB10")]
public interface ICLRDebugging
{
	[PreserveSig]
	int OpenVirtualProcess([In] ulong moduleBaseAddress, [In][MarshalAs(UnmanagedType.IUnknown)] object dataTarget, [In][MarshalAs(UnmanagedType.Interface)] ICLRDebuggingLibraryProvider libraryProvider, [In] ref ClrDebuggingVersion maxDebuggerSupportedVersion, [In] ref Guid riidProcess, [MarshalAs(UnmanagedType.IUnknown)] out object process, [In][Out] ref ClrDebuggingVersion version, out ClrDebuggingProcessFlags flags);

	[PreserveSig]
	int CanUnloadNow(IntPtr moduleHandle);
}
