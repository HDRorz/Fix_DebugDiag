using System.Runtime.InteropServices;

namespace PEFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IMAGE_NT_HEADERS
{
	internal uint Signature;

	internal IMAGE_FILE_HEADER FileHeader;
}
