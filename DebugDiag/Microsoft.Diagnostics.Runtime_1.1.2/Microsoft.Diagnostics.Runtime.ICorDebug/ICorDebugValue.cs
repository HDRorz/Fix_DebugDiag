using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAF7-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugValue
{
	void GetType(out CorElementType pType);

	void GetSize(out uint pSize);

	void GetAddress(out ulong pAddress);

	void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
}
