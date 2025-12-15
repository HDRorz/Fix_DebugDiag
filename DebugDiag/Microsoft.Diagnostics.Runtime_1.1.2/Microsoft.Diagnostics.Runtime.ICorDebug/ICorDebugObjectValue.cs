using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("18AD3D6E-B7D2-11D2-BD04-0000F80849BD")]
public interface ICorDebugObjectValue : ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

	void GetFieldValue([In][MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass, [In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetVirtualMethod([In] uint memberRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

	void IsValueClass(out int pbIsValueClass);

	void GetManagedCopy([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);

	void SetFromManagedCopy([In][MarshalAs(UnmanagedType.IUnknown)] object pObject);
}
