using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3151C08D-4D09-4f9b-8838-2880BF18FE51")]
public interface ICLRDebuggingLibraryProvider
{
	[PreserveSig]
	int ProvideLibrary([In][MarshalAs(UnmanagedType.LPWStr)] string fileName, int timestamp, int sizeOfImage, out IntPtr hModule);
}
