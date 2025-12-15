using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("3D6F5F62-7538-11D3-8D5B-00104B35E7EF")]
[InterfaceType(1)]
public interface ICorDebugController
{
	void Stop([In] uint dwTimeout);

	void Continue([In] int fIsOutOfBand);

	void IsRunning(out int pbRunning);

	void HasQueuedCallbacks([In][MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, out int pbQueued);

	void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);

	void SetAllThreadsDebugState([In] CorDebugThreadState state, [In][MarshalAs(UnmanagedType.Interface)] ICorDebugThread pExceptThisThread);

	void Detach();

	void Terminate([In] uint exitCode);

	void CanCommitChanges([In] uint cSnapshots, [In][MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

	void CommitChanges([In] uint cSnapshots, [In][MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
}
