using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrThreadPool
{
	public abstract int TotalThreads { get; }

	public abstract int RunningThreads { get; }

	public abstract int IdleThreads { get; }

	public abstract int MinThreads { get; }

	public abstract int MaxThreads { get; }

	public abstract int MinCompletionPorts { get; }

	public abstract int MaxCompletionPorts { get; }

	public abstract int CpuUtilization { get; }

	public abstract int FreeCompletionPortCount { get; }

	public abstract int MaxFreeCompletionPorts { get; }

	public abstract IEnumerable<NativeWorkItem> EnumerateNativeWorkItems();

	public abstract IEnumerable<ManagedWorkItem> EnumerateManagedWorkItems();
}
