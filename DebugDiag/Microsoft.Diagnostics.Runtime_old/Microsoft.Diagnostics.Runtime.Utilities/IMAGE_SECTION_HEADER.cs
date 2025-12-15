using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct IMAGE_SECTION_HEADER
{
	public unsafe fixed byte NameBytes[8];

	public uint VirtualSize;

	public uint VirtualAddress;

	public uint SizeOfRawData;

	public uint PointerToRawData;

	public uint PointerToRelocations;

	public uint PointerToLinenumbers;

	public ushort NumberOfRelocations;

	public ushort NumberOfLinenumbers;

	public uint Characteristics;

	public unsafe string Name
	{
		get
		{
			fixed (byte* nameBytes = NameBytes)
			{
				if (nameBytes[7] == 0)
				{
					return Marshal.PtrToStringAnsi((IntPtr)nameBytes);
				}
				return Marshal.PtrToStringAnsi((IntPtr)nameBytes, 8);
			}
		}
	}
}
