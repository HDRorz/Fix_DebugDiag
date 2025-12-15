namespace PEFile;

internal struct IMAGE_DEBUG_DIRECTORY
{
	internal int Characteristics;

	internal int TimeDateStamp;

	internal short MajorVersion;

	internal short MinorVersion;

	internal IMAGE_DEBUG_TYPE Type;

	internal int SizeOfData;

	internal int AddressOfRawData;

	internal int PointerToRawData;
}
