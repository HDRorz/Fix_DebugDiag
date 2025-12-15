using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("03E26314-4F76-11D3-88C6-006097945418")]
public interface ICorDebugNativeFrame : ICorDebugFrame
{
	new void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	new void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

	new void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	new void GetFunctionToken(out uint pToken);

	new void GetStackRange(out ulong pStart, out ulong pEnd);

	new void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	new void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

	void GetIP(out uint pnOffset);

	void SetIP([In] uint nOffset);

	void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);

	void GetLocalRegisterValue([In] CorDebugRegister reg, [In] uint cbSigBlob, [In][ComAliasName("Microsoft.Debugging.CorDebug.NativeApi.ULONG_PTR")] uint pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetLocalDoubleRegisterValue([In] CorDebugRegister highWordReg, [In] CorDebugRegister lowWordReg, [In] uint cbSigBlob, [In][ComAliasName("Microsoft.Debugging.CorDebug.NativeApi.ULONG_PTR")] uint pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetLocalMemoryValue([In] ulong address, [In] uint cbSigBlob, [In][ComAliasName("Microsoft.Debugging.CorDebug.NativeApi.ULONG_PTR")] uint pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetLocalRegisterMemoryValue([In] CorDebugRegister highWordReg, [In] ulong lowWordAddress, [In] uint cbSigBlob, [In][ComAliasName("Microsoft.Debugging.CorDebug.NativeApi.ULONG_PTR")] uint pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetLocalMemoryRegisterValue([In] ulong highWordAddress, [In] CorDebugRegister lowWordRegister, [In] uint cbSigBlob, [In][ComAliasName("Microsoft.Debugging.CorDebug.NativeApi.ULONG_PTR")] uint pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int CanSetIP([In] uint nOffset);
}
