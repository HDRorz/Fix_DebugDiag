using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("CC7BCAF9-8A68-11D2-983C-0000F808342D")]
public interface ICorDebugReferenceValue : ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void IsNull(out int pbNull);

	void GetValue(out ulong pValue);

	void SetValue([In] ulong value);

	void Dereference([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void DereferenceStrong([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
}
