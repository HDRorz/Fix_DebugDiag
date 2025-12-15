using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("CC7BCAF8-8A68-11D2-983C-0000F808342D")]
public interface ICorDebugGenericValue : ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void GetValue([Out] IntPtr pTo);

	void SetValue([In] IntPtr pFrom);
}
