using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DebugDiag.DotNet;

internal static class ComHelper
{
	private delegate int DllGetClassObject(ref Guid clsid, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IClassFactory classFactory);

	internal static object CreateInstance(LibraryModule libraryModule, Guid clsid)
	{
		IClassFactory classFactory = GetClassFactory(libraryModule, clsid);
		Guid riid = new Guid("00000000-0000-0000-C000-000000000046");
		classFactory.CreateInstance(null, ref riid, out var ppvObject);
		return ppvObject;
	}

	internal static IClassFactory GetClassFactory(LibraryModule libraryModule, Guid clsid)
	{
		DllGetClassObject obj = (DllGetClassObject)Marshal.GetDelegateForFunctionPointer(libraryModule.GetProcAddress("DllGetClassObject"), typeof(DllGetClassObject));
		Guid iid = new Guid("00000001-0000-0000-c000-000000000046");
		IClassFactory classFactory;
		int num = obj(ref clsid, ref iid, out classFactory);
		if (num != 0)
		{
			throw new Win32Exception(num, "Cannot create class factory");
		}
		return classFactory;
	}
}
