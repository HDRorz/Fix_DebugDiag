using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000100-0000-0000-C000-000000000046")]
internal interface IEnumUnknown
{
	[PreserveSig]
	int Next([In][MarshalAs(UnmanagedType.U4)] int celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, IntPtr pceltFetched);

	[PreserveSig]
	int Skip([In][MarshalAs(UnmanagedType.U4)] int celt);

	void Reset();

	void Clone(out IEnumUnknown ppenum);
}
