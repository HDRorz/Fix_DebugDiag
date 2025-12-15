using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[InterfaceType(1)]
[Guid("3D6F5F64-7538-11D3-8D5B-00104B35E7EF")]
public interface ICorDebugProcess : ICorDebugController
{
	new void Stop([In] uint dwTimeout);

	new void Continue([In] int fIsOutOfBand);

	new void IsRunning(out int pbRunning);

	new void HasQueuedCallbacks([In][MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, out int pbQueued);

	new void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);

	new void SetAllThreadsDebugState([In] CorDebugThreadState state, [In][MarshalAs(UnmanagedType.Interface)] ICorDebugThread pExceptThisThread);

	new void Detach();

	new void Terminate([In] uint exitCode);

	new void CanCommitChanges([In] uint cSnapshots, [In][MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

	new void CommitChanges([In] uint cSnapshots, [In][MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

	void GetID(out uint pdwProcessId);

	void GetHandle([ComAliasName("HPROCESS*")] out IntPtr phProcessHandle);

	void GetThread([In] uint dwThreadId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

	void EnumerateObjects([MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectEnum ppObjects);

	void IsTransitionStub([In] ulong address, out int pbTransitionStub);

	void IsOSSuspended([In] uint threadID, out int pbSuspended);

	void GetThreadContext([In] uint threadID, [In] uint contextSize, [In][ComAliasName("BYTE*")] IntPtr context);

	void SetThreadContext([In] uint threadID, [In] uint contextSize, [In][ComAliasName("BYTE*")] IntPtr context);

	void ReadMemory([In] ulong address, [In] uint size, [Out][MarshalAs(UnmanagedType.LPArray)] byte[] buffer, [ComAliasName("SIZE_T*")] out IntPtr read);

	void WriteMemory([In] ulong address, [In] uint size, [In][MarshalAs(UnmanagedType.LPArray)] byte[] buffer, [ComAliasName("SIZE_T*")] out IntPtr written);

	void ClearCurrentException([In] uint threadID);

	void EnableLogMessages([In] int fOnOff);

	void ModifyLogSwitch([In][MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In] int lLevel);

	void EnumerateAppDomains([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomainEnum ppAppDomains);

	void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);

	void ThreadForFiberCookie([In] uint fiberCookie, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

	void GetHelperThreadID(out uint pThreadID);
}
