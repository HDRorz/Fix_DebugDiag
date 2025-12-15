using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrThread
{
	public abstract ClrRuntime Runtime { get; }

	public abstract GcMode GcMode { get; }

	public abstract bool IsFinalizer { get; }

	public abstract ulong Address { get; }

	public abstract bool IsAlive { get; }

	public abstract uint OSThreadId { get; }

	public abstract int ManagedThreadId { get; }

	public abstract ulong AppDomain { get; }

	public abstract uint LockCount { get; }

	public abstract ulong Teb { get; }

	public abstract ulong StackBase { get; }

	public abstract ulong StackLimit { get; }

	public abstract IList<ClrStackFrame> StackTrace { get; }

	public abstract ClrException CurrentException { get; }

	public abstract bool IsGC { get; }

	public abstract bool IsDebuggerHelper { get; }

	public abstract bool IsThreadpoolTimer { get; }

	public abstract bool IsThreadpoolCompletionPort { get; }

	public abstract bool IsThreadpoolWorker { get; }

	public abstract bool IsThreadpoolWait { get; }

	public abstract bool IsThreadpoolGate { get; }

	public abstract bool IsSuspendingEE { get; }

	public abstract bool IsShutdownHelper { get; }

	public abstract bool IsAbortRequested { get; }

	public abstract bool IsAborted { get; }

	public abstract bool IsGCSuspendPending { get; }

	public abstract bool IsUserSuspended { get; }

	public abstract bool IsDebugSuspended { get; }

	public abstract bool IsBackground { get; }

	public abstract bool IsUnstarted { get; }

	public abstract bool IsCoInitialized { get; }

	public abstract bool IsSTA { get; }

	public abstract bool IsMTA { get; }

	[Obsolete]
	public abstract IList<BlockingObject> BlockingObjects { get; }

	public abstract IEnumerable<ClrRoot> EnumerateStackObjects();

	public abstract IEnumerable<ClrRoot> EnumerateStackObjects(bool includePossiblyDead);

	internal static bool GetExactPolicy(ClrRuntime runtime, ClrRootStackwalkPolicy stackwalkPolicy)
	{
		switch (stackwalkPolicy)
		{
		case ClrRootStackwalkPolicy.Automatic:
			if (runtime.Threads.Count >= 512)
			{
				return false;
			}
			return true;
		case ClrRootStackwalkPolicy.Exact:
			return true;
		default:
			return false;
		}
	}

	public abstract IEnumerable<ClrStackFrame> EnumerateStackTrace();
}
