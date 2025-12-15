using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D332DB9E-B9B3-4125-8207-A14884F53216")]
internal interface ICLRMetaHost
{
	[return: MarshalAs(UnmanagedType.Interface)]
	object GetRuntime([In][MarshalAs(UnmanagedType.LPWStr)] string pwzVersion, [In] ref Guid riid);

	void GetVersionFromFile([In][MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, [In][Out] ref uint pcchBuffer);

	[return: MarshalAs(UnmanagedType.Interface)]
	IEnumUnknown EnumerateInstalledRuntimes();

	[return: MarshalAs(UnmanagedType.Interface)]
	IEnumUnknown EnumerateLoadedRuntimes([In] ProcessSafeHandle hndProcess);
}
