using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("D613F0BB-ACE1-4C19-BD72-E4C08D5DA7F5")]
[InterfaceType(1)]
public interface ICorDebugType
{
	void GetType(out CorElementType ty);

	void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

	void EnumerateTypeParameters([MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTyParEnum);

	void GetFirstTypeParameter([MarshalAs(UnmanagedType.Interface)] out ICorDebugType value);

	void GetBase([MarshalAs(UnmanagedType.Interface)] out ICorDebugType pBase);

	void GetStaticFieldValue([In] uint fieldDef, [In][MarshalAs(UnmanagedType.Interface)] ICorDebugFrame pFrame, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetRank(out uint pnRank);
}
