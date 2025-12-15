using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("938C6D66-7FB6-4F69-B389-425B8987329B")]
[InterfaceType(1)]
public interface ICorDebugThread
{
	void GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

	void GetID(out uint pdwThreadId);

	void GetHandle(out IntPtr phThreadHandle);

	void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);

	void SetDebugState([In] CorDebugThreadState state);

	void GetDebugState(out CorDebugThreadState pState);

	void GetUserState(out CorDebugUserState pState);

	void GetCurrentException([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppExceptionObject);

	void ClearCurrentException();

	void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

	void EnumerateChains([MarshalAs(UnmanagedType.Interface)] out ICorDebugChainEnum ppChains);

	void GetActiveChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void GetActiveFrame([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);

	void CreateEval([MarshalAs(UnmanagedType.Interface)] out ICorDebugEval ppEval);

	void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);
}
