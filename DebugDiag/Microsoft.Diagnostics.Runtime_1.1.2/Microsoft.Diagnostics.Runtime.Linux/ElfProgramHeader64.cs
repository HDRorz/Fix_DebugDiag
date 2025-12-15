using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfProgramHeader64
{
	public ElfProgramHeaderType Type;

	public uint Flags;

	public ulong FileOffset;

	public ulong VirtualAddress;

	public ulong PhysicalAddress;

	public ulong FileSize;

	public ulong VirtualSize;

	public ulong Alignment;
}
