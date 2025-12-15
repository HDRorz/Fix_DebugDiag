using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAF3-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugFunction
{
	void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);

	void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

	void GetToken(out uint pMethodDef);

	void GetILCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	void GetNativeCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);

	void GetLocalVarSigToken(out uint pmdSig);

	void GetCurrentVersionNumber(out uint pnCurrentVersion);
}
