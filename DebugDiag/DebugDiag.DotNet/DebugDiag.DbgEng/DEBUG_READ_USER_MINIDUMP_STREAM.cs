using System;

namespace DebugDiag.DbgEng;

public struct DEBUG_READ_USER_MINIDUMP_STREAM
{
	public MINIDUMP_STREAM_TYPE StreamType;

	public uint Flags;

	public ulong Offset;

	public IntPtr Buffer;

	public uint BufferSize;

	public uint BufferUsed;
}
