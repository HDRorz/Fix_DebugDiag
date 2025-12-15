namespace PEFile;

internal struct IMAGE_RESOURCE_DIRECTORY
{
	internal int Characteristics;

	internal int TimeDateStamp;

	internal short MajorVersion;

	internal short MinorVersion;

	internal ushort NumberOfNamedEntries;

	internal ushort NumberOfIdEntries;
}
