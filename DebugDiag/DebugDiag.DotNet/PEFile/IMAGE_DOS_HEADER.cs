using System.Runtime.InteropServices;

namespace PEFile;

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct IMAGE_DOS_HEADER
{
	internal const short IMAGE_DOS_SIGNATURE = 23117;

	[FieldOffset(0)]
	internal short e_magic;

	[FieldOffset(60)]
	internal int e_lfanew;
}
