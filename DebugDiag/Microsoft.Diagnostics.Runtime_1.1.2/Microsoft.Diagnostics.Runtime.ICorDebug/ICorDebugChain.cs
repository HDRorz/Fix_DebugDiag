using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAEE-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugChain
{
	void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

	void GetStackRange(out ulong pStart, out ulong pEnd);

	void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

	void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void GetPrevious([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void GetNext([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

	void IsManaged(out int pManaged);

	void EnumerateFrames([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrameEnum ppFrames);

	void GetActiveFrame([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

	void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);

	void GetReason(out CorDebugChainReason pReason);
}
