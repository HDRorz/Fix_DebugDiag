using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal static class DumpUtility
{
	[StructLayout(LayoutKind.Explicit)]
	private struct IMAGE_DOS_HEADER
	{
		[FieldOffset(0)]
		public short e_magic;

		[FieldOffset(60)]
		public uint e_lfanew;

		public bool IsValid => e_magic == 23117;
	}

	private struct IMAGE_FILE_HEADER
	{
		public short Machine;

		public short NumberOfSections;

		public uint TimeDateStamp;

		public uint PointerToSymbolTable;

		public uint NumberOfSymbols;

		public short SizeOfOptionalHeader;

		public short Characteristics;
	}

	private struct IMAGE_NT_HEADERS
	{
		public uint Signature;

		public IMAGE_FILE_HEADER FileHeader;
	}

	private static T MarshalAt<T>(byte[] buffer, uint offset)
	{
		int num = Marshal.SizeOf(typeof(T));
		if (offset + num > buffer.Length)
		{
			throw new ArgumentOutOfRangeException();
		}
		GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		IntPtr ptr = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + offset);
		T val = default(T);
		Marshal.PtrToStructure(ptr, val);
		gCHandle.Free();
		return val;
	}

	public static uint GetTimestamp(string file)
	{
		if (!File.Exists(file))
		{
			return 0u;
		}
		byte[] buffer = File.ReadAllBytes(file);
		IMAGE_DOS_HEADER iMAGE_DOS_HEADER = MarshalAt<IMAGE_DOS_HEADER>(buffer, 0u);
		if (!iMAGE_DOS_HEADER.IsValid)
		{
			return 0u;
		}
		uint e_lfanew = iMAGE_DOS_HEADER.e_lfanew;
		return MarshalAt<IMAGE_NT_HEADERS>(buffer, e_lfanew).FileHeader.TimeDateStamp;
	}
}
