using System.Runtime.InteropServices;

namespace PEFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IMAGE_OPTIONAL_HEADER32
{
	internal ushort Magic;

	internal byte MajorLinkerVersion;

	internal byte MinorLinkerVersion;

	internal uint SizeOfCode;

	internal uint SizeOfInitializedData;

	internal uint SizeOfUninitializedData;

	internal uint AddressOfEntryPoint;

	internal uint BaseOfCode;

	internal uint BaseOfData;

	internal uint ImageBase;

	internal uint SectionAlignment;

	internal uint FileAlignment;

	internal ushort MajorOperatingSystemVersion;

	internal ushort MinorOperatingSystemVersion;

	internal ushort MajorImageVersion;

	internal ushort MinorImageVersion;

	internal ushort MajorSubsystemVersion;

	internal ushort MinorSubsystemVersion;

	internal uint Win32VersionValue;

	internal uint SizeOfImage;

	internal uint SizeOfHeaders;

	internal uint CheckSum;

	internal ushort Subsystem;

	internal ushort DllCharacteristics;

	internal uint SizeOfStackReserve;

	internal uint SizeOfStackCommit;

	internal uint SizeOfHeapReserve;

	internal uint SizeOfHeapCommit;

	internal uint LoaderFlags;

	internal uint NumberOfRvaAndSizes;
}
