using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class DumpThread
{
	private readonly DumpReader _owner;

	private readonly MINIDUMP_THREAD _raw;

	public ulong Teb => _raw.Teb;

	public int ThreadId => (int)_raw.ThreadId;

	internal DumpThread(DumpReader owner, MINIDUMP_THREAD raw)
	{
		_raw = raw;
		_owner = owner;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is DumpThread dumpThread))
		{
			return false;
		}
		if (dumpThread._owner == _owner)
		{
			return dumpThread._raw == _raw;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ThreadId;
	}

	public override string ToString()
	{
		int threadId = ThreadId;
		return string.Format(CultureInfo.CurrentUICulture, "Thread {0} (0x{0:x})", new object[1] { threadId });
	}

	public void GetThreadContext(IntPtr buffer, int sizeBufferBytes)
	{
		_owner.GetThreadContext(_raw.ThreadContext, buffer, sizeBufferBytes);
	}
}
