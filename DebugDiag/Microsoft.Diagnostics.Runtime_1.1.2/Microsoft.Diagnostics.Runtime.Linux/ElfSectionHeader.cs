namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfSectionHeader
{
	public ElfSectionHeaderType Type { get; }

	public int NameIndex { get; }

	public ulong VirtualAddress { get; }

	public ulong FileOffset { get; }

	public ulong FileSize { get; }

	public ElfSectionHeader(Reader reader, bool is64bit, long headerPositon)
	{
		if (is64bit)
		{
			ElfSectionHeader64 elfSectionHeader = reader.Read<ElfSectionHeader64>(headerPositon);
			Type = elfSectionHeader.Type;
			NameIndex = elfSectionHeader.NameIndex;
			VirtualAddress = elfSectionHeader.VirtualAddress;
			FileOffset = elfSectionHeader.FileOffset;
			FileSize = elfSectionHeader.FileSize;
		}
		else
		{
			ElfSectionHeader32 elfSectionHeader2 = reader.Read<ElfSectionHeader32>(headerPositon);
			Type = elfSectionHeader2.Type;
			NameIndex = elfSectionHeader2.NameIndex;
			VirtualAddress = elfSectionHeader2.VirtualAddress;
			FileOffset = elfSectionHeader2.FileOffset;
			FileSize = elfSectionHeader2.FileSize;
		}
	}
}
