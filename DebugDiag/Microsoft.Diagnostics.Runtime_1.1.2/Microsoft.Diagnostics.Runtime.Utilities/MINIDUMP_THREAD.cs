using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[StructLayout(LayoutKind.Sequential)]
internal class MINIDUMP_THREAD
{
	public uint ThreadId;

	public uint SuspendCount;

	public uint PriorityClass;

	public uint Priority;

	private ulong _teb;

	public MINIDUMP_MEMORY_DESCRIPTOR Stack;

	public MINIDUMP_LOCATION_DESCRIPTOR ThreadContext;

	public ulong Teb => DumpNative.ZeroExtendAddress(_teb);

	public virtual MINIDUMP_MEMORY_DESCRIPTOR BackingStore
	{
		get
		{
			throw new MissingMemberException("MINIDUMP_THREAD has no backing store!");
		}
		set
		{
			throw new MissingMemberException("MINIDUMP_THREAD has no backing store!");
		}
	}

	public virtual bool HasBackingStore()
	{
		return false;
	}
}
