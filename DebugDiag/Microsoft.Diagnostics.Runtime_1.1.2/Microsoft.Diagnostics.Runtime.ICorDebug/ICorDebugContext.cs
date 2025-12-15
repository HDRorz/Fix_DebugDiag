using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("CC7BCB00-8A68-11D2-983C-0000F808342D")]
public interface ICorDebugContext : ICorDebugObjectValue, ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	new void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

	new void GetFieldValue([In][MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass, [In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	new void GetVirtualMethod([In] uint memberRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	new void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

	new void IsValueClass(out int pbIsValueClass);

	new void GetManagedCopy([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);

	new void SetFromManagedCopy([In][MarshalAs(UnmanagedType.IUnknown)] object pObject);
}
