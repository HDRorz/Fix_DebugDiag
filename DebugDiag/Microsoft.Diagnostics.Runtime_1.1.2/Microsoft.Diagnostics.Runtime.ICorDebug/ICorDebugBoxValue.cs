using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("CC7BCAFC-8A68-11D2-983C-0000F808342D")]
public interface ICorDebugBoxValue : ICorDebugHeapValue, ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	new void IsValid(out int pbValid);

	new void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectValue ppObject);
}
