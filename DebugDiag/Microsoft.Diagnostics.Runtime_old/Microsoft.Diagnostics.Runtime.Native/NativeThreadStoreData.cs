using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeThreadStoreData : IThreadStoreData
{
	public int threadCount;

	public ulong firstThread;

	public ulong finalizerThread;

	public ulong gcThread;

	public ulong Finalizer => finalizerThread;

	public ulong FirstThread => firstThread;

	public int Count => threadCount;
}
