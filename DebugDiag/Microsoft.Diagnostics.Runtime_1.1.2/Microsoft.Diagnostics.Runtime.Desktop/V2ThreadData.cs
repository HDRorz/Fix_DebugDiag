using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V2ThreadData : IThreadData
{
	public readonly uint CorThreadId;

	public readonly uint OSThreadId;

	public readonly int State;

	public readonly uint PreemptiveGCDisabled;

	public readonly ulong AllocContextPtr;

	public readonly ulong AllocContextLimit;

	public readonly ulong Context;

	public readonly ulong Domain;

	public readonly ulong SharedStaticData;

	public readonly ulong UnsharedStaticData;

	public readonly ulong Frame;

	public readonly uint LockCount;

	public readonly ulong FirstNestedException;

	public readonly ulong Teb;

	public readonly ulong FiberData;

	public readonly ulong LastThrownObjectHandle;

	public readonly ulong NextThread;

	ulong IThreadData.Next
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)NextThread;
			}
			return NextThread;
		}
	}

	ulong IThreadData.AllocPtr
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)AllocContextPtr;
			}
			return AllocContextPtr;
		}
	}

	ulong IThreadData.AllocLimit
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)AllocContextLimit;
			}
			return AllocContextLimit;
		}
	}

	uint IThreadData.OSThreadID => OSThreadId;

	ulong IThreadData.Teb
	{
		get
		{
			if (IntPtr.Size != 8)
			{
				return (uint)Teb;
			}
			return Teb;
		}
	}

	ulong IThreadData.AppDomain => Domain;

	uint IThreadData.LockCount => LockCount;

	int IThreadData.State => State;

	ulong IThreadData.ExceptionPtr => LastThrownObjectHandle;

	uint IThreadData.ManagedThreadID => CorThreadId;

	bool IThreadData.Preemptive => PreemptiveGCDisabled == 0;
}
