using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAFA-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugHeapValue : ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void IsValid(out int pbValid);

	void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
}
