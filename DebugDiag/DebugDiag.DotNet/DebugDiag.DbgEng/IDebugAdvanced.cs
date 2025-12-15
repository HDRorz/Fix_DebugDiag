using System;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("f2df5f53-071f-47bd-9de6-5734c3fed689")]
public interface IDebugAdvanced
{
	[PreserveSig]
	int GetThreadContext([In] IntPtr Context, [In] uint ContextSize);

	[PreserveSig]
	int SetThreadContext([In] IntPtr Context, [In] uint ContextSize);
}
