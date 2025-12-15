using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfSectionHeader64
{
	public int NameIndex;

	public ElfSectionHeaderType Type;

	public ulong Flags;

	public ulong VirtualAddress;

	public ulong FileOffset;

	public ulong FileSize;

	public uint Link;

	public uint Info;

	public ulong Alignment;

	public ulong EntrySize;
}
