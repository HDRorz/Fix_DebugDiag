using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfProgramHeader32
{
	public ElfProgramHeaderType Type;

	public uint FileOffset;

	public uint VirtualAddress;

	public uint PhysicalAddress;

	public uint FileSize;

	public uint VirtualSize;

	public uint Flags;

	public uint Alignment;
}
