using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfSectionHeader32
{
	public int NameIndex;

	public ElfSectionHeaderType Type;

	public uint Flags;

	public uint VirtualAddress;

	public uint FileOffset;

	public uint FileSize;

	public uint Link;

	public uint Info;

	public uint Alignment;

	public uint EntrySize;
}
