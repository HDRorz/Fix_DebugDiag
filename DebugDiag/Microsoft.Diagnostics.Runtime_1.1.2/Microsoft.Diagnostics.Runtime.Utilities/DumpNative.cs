using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal static class DumpNative
{
	private const uint MINIDUMP_SIGNATURE = 1347241037u;

	private const uint MINIDUMP_VERSION = 42899u;

	private const uint MiniDumpWithFullMemoryInfo = 2u;

	public const uint EXCEPTION_MAXIMUM_PARAMETERS = 15u;

	public static ulong ZeroExtendAddress(ulong addr)
	{
		if (IntPtr.Size == 4)
		{
			return addr &= 0xFFFFFFFFu;
		}
		return addr;
	}

	public static bool IsMiniDump(IntPtr pbase)
	{
		return (((MINIDUMP_HEADER)Marshal.PtrToStructure(pbase, typeof(MINIDUMP_HEADER))).Flags & 2) == 0;
	}

	public static bool MiniDumpReadDumpStream(IntPtr pBase, MINIDUMP_STREAM_TYPE type, out IntPtr streamPointer, out uint cbStreamSize)
	{
		MINIDUMP_HEADER mINIDUMP_HEADER = (MINIDUMP_HEADER)Marshal.PtrToStructure(pBase, typeof(MINIDUMP_HEADER));
		streamPointer = IntPtr.Zero;
		cbStreamSize = 0u;
		if (mINIDUMP_HEADER.Singature != 1347241037 || (mINIDUMP_HEADER.Version & 0xFFFF) != 42899)
		{
			return false;
		}
		int num = Marshal.SizeOf(typeof(MINIDUMP_DIRECTORY));
		long num2 = pBase.ToInt64() + (int)mINIDUMP_HEADER.StreamDirectoryRva;
		for (int i = 0; i < (int)mINIDUMP_HEADER.NumberOfStreams; i++)
		{
			MINIDUMP_DIRECTORY mINIDUMP_DIRECTORY = (MINIDUMP_DIRECTORY)Marshal.PtrToStructure(new IntPtr(num2 + i * num), typeof(MINIDUMP_DIRECTORY));
			if (mINIDUMP_DIRECTORY.StreamType == type)
			{
				streamPointer = new IntPtr(pBase.ToInt64() + (int)mINIDUMP_DIRECTORY.Rva);
				cbStreamSize = mINIDUMP_DIRECTORY.DataSize;
				return true;
			}
		}
		return false;
	}
}
