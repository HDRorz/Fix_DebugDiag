using System.Runtime.InteropServices;

namespace PEFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IMAGE_FILE_HEADER
{
	internal ushort Machine;

	internal ushort NumberOfSections;

	internal uint TimeDateStamp;

	internal uint PointerToSymbolTable;

	internal uint NumberOfSymbols;

	internal ushort SizeOfOptionalHeader;

	internal ushort Characteristics;
}
