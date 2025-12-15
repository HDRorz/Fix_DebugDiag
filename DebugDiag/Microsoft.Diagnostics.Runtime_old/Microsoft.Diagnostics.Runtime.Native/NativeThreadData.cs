using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeThreadData : IThreadData
{
	public uint osThreadId;

	public int state;

	public uint preemptiveGCDisabled;

	public ulong allocContextPtr;

	public ulong allocContextLimit;

	public ulong context;

	public ulong teb;

	public ulong nextThread;

	public ulong Next => nextThread;

	public ulong AllocPtr => allocContextPtr;

	public ulong AllocLimit => allocContextLimit;

	public uint OSThreadID => osThreadId;

	public ulong Teb => teb;

	public ulong AppDomain => 0uL;

	public uint LockCount => 0u;

	public int State => state;

	public ulong ExceptionPtr => 0uL;

	public uint ManagedThreadID => osThreadId;

	public bool Preemptive => preemptiveGCDisabled == 0;
}
