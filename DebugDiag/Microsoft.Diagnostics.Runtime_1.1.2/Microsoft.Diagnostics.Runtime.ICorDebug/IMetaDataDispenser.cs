using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("809C652E-7396-11D2-9771-00A0C9B4D50C")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMetaDataDispenser
{
	[PreserveSig]
	void DefineScope([In] ref Guid rclsid, [In] uint dwCreateFlags, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIUnk);

	[PreserveSig]
	void OpenScope([In][MarshalAs(UnmanagedType.LPWStr)] string szScope, [In] int dwOpenFlags, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIUnk);

	[PreserveSig]
	void OpenScopeOnMemory([In] IntPtr pData, [In] uint cbData, [In] int dwOpenFlags, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
}
