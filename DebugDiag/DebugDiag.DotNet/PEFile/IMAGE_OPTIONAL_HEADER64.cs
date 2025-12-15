using System.Runtime.InteropServices;

namespace PEFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IMAGE_OPTIONAL_HEADER64
{
	internal ushort Magic;

	internal byte MajorLinkerVersion;

	internal byte MinorLinkerVersion;

	internal uint SizeOfCode;

	internal uint SizeOfInitializedData;

	internal uint SizeOfUninitializedData;

	internal uint AddressOfEntryPoint;

	internal uint BaseOfCode;

	internal ulong ImageBase;

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

	internal ulong SizeOfStackReserve;

	internal ulong SizeOfStackCommit;

	internal ulong SizeOfHeapReserve;

	internal ulong SizeOfHeapCommit;

	internal uint LoaderFlags;

	internal uint NumberOfRvaAndSizes;
}
