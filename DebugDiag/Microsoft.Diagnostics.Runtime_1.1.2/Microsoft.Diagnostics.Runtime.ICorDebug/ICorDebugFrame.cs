using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAEF-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugFrame
{
	void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetFunctionToken(out uint pToken);

	void GetStackRange(out ulong pStart, out ulong pEnd);

	void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
}
