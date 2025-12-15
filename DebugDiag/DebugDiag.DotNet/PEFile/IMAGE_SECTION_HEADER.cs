using System;
using System.Runtime.InteropServices;

namespace PEFile;

internal struct IMAGE_SECTION_HEADER
{
	internal unsafe fixed byte NameBytes[8];

	internal uint VirtualSize;

	internal uint VirtualAddress;

	internal uint SizeOfRawData;

	internal uint PointerToRawData;

	internal uint PointerToRelocations;

	internal uint PointerToLinenumbers;

	internal ushort NumberOfRelocations;

	internal ushort NumberOfLinenumbers;

	internal uint Characteristics;

	internal unsafe string Name
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
