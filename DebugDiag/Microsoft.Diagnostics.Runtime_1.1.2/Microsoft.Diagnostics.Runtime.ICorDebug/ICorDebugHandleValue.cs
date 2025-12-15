using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("029596E8-276B-46A1-9821-732E96BBB00B")]
[InterfaceType(1)]
public interface ICorDebugHandleValue : ICorDebugReferenceValue, ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	new void IsNull(out int pbNull);

	new void GetValue(out ulong pValue);

	new void SetValue([In] ulong value);

	new void Dereference([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	new void DereferenceStrong([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetHandleType(out CorDebugHandleType pType);

	void Dispose();
}
