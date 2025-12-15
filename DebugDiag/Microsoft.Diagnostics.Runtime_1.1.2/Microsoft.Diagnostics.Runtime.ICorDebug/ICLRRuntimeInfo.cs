using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
internal interface ICLRRuntimeInfo
{
	void GetVersionString([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, [In][Out][MarshalAs(UnmanagedType.U4)] ref int pcchBuffer);

	[PreserveSig]
	int GetRuntimeDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, [In][Out][MarshalAs(UnmanagedType.U4)] ref int pcchBuffer);

	int IsLoaded([In] IntPtr hndProcess);

	void LoadErrorString([In][MarshalAs(UnmanagedType.U4)] int iResourceID, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, [In][Out][MarshalAs(UnmanagedType.U4)] ref int pcchBuffer, [In] int iLocaleID);

	IntPtr LoadLibrary([In][MarshalAs(UnmanagedType.LPWStr)] string pwzDllName);

	IntPtr GetProcAddress([In][MarshalAs(UnmanagedType.LPStr)] string pszProcName);

	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetInterface([In] ref Guid rclsid, [In] ref Guid riid);
}
