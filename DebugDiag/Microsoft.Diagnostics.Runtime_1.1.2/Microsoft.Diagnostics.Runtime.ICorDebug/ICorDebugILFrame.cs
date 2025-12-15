using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("03E26311-4F76-11D3-88C6-006097945418")]
public interface ICorDebugILFrame : ICorDebugFrame
{
	new void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	new void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	new void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	new void GetFunctionToken(out uint pToken);

	new void GetStackRange(out ulong pStart, out ulong pEnd);

	new void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

	void GetIP(out uint pnOffset, out CorDebugMappingResult pMappingResult);

	void SetIP([In] uint nOffset);

	void EnumerateLocalVariables([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);

	void GetLocalVariable([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void EnumerateArguments([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);

	void GetArgument([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetStackDepth(out uint pDepth);

	void GetStackValue([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int CanSetIP([In] uint nOffset);
}
