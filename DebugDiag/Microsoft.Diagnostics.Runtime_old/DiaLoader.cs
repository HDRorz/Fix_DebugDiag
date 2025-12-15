using System;
using System.Runtime.InteropServices;
using Dia2Lib;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Desktop;

internal static class DiaLoader
{
	[ComImport]
	[ComVisible(false)]
	[Guid("00000001-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IClassFactory
	{
		void CreateInstance([MarshalAs(UnmanagedType.Interface)] object aggregator, ref Guid refiid, [MarshalAs(UnmanagedType.Interface)] out object createdObject);

		void LockServer(bool incrementRefCount);
	}

	public static IDiaDataSource GetDiaSourceObject()
	{
		if (!NativeMethods.LoadNative("msdia120.dll"))
		{
			throw new ClrDiagnosticsException("Could not load native DLL msdia120.dll HRESULT=0x" + Marshal.GetHRForLastWin32Error().ToString("x"), ClrDiagnosticsException.HR.ApplicationError);
		}
		IClassFactory obj = (IClassFactory)DllGetClassObject(new Guid("{3BFCEA48-620F-4B6B-81F7-B9AF75454C7D}"), typeof(IClassFactory).GUID);
		object createdObject = null;
		Guid refiid = typeof(IDiaDataSource).GUID;
		obj.CreateInstance(null, ref refiid, out createdObject);
		return (IDiaDataSource)((createdObject is IDiaDataSource) ? createdObject : null);
	}

	[DllImport("msdia120.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
	[return: MarshalAs(UnmanagedType.Interface)]
	private static extern object DllGetClassObject([In][MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid);
}
