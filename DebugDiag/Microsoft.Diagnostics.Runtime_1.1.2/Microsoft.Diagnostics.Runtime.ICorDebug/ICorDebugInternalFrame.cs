using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("B92CC7F7-9D2D-45C4-BC2B-621FCC9DFBF4")]
public interface ICorDebugInternalFrame : ICorDebugFrame
{
	new void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	new void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	new void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	new void GetFunctionToken(out uint pToken);

	new void GetStackRange(out ulong pStart, out ulong pEnd);

	new void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

	void GetFrameType(out CorDebugInternalFrameType pType);
}
