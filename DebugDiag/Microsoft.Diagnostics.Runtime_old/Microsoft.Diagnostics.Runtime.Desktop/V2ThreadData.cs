using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V2ThreadData : IThreadData
{
	public uint corThreadId;

	public uint osThreadId;

	public int state;

	public uint preemptiveGCDisabled;

	public ulong allocContextPtr;

	public ulong allocContextLimit;

	public ulong context;

	public ulong domain;

	public ulong sharedStaticData;

	public ulong unsharedStaticData;

	public ulong pFrame;

	public uint lockCount;

	public ulong firstNestedException;

	public ulong teb;

	public ulong fiberData;

	public ulong lastThrownObjectHandle;

	public ulong nextThread;

	public ulong Next
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)nextThread;
			}
			return nextThread;
		}
	}

	public ulong AllocPtr
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)allocContextPtr;
			}
			return allocContextPtr;
		}
	}

	public ulong AllocLimit
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)allocContextLimit;
			}
			return allocContextLimit;
		}
	}

	public uint OSThreadID => osThreadId;

	public ulong Teb
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)teb;
			}
			return teb;
		}
	}

	public ulong AppDomain => domain;

	public uint LockCount => lockCount;

	public int State => state;

	public ulong ExceptionPtr => lastThrownObjectHandle;

	public uint ManagedThreadID => corThreadId;

	public bool Preemptive => preemptiveGCDisabled == 0;
}
