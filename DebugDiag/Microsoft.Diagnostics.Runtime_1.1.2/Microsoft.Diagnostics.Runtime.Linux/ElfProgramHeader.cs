namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfProgramHeader
{
	private readonly ElfProgramHeaderAttributes _attributes;

	public IAddressSpace AddressSpace { get; }

	public ElfProgramHeaderType Type { get; }

	public long VirtualAddress { get; }

	public long VirtualSize { get; }

	public long FileOffset { get; }

	public long FileSize { get; }

	public bool IsWritable => (_attributes & ElfProgramHeaderAttributes.Writable) != 0;

	public ElfProgramHeader(Reader reader, bool is64bit, long headerPositon, long fileOffset, bool isVirtual = false)
	{
		if (is64bit)
		{
			ElfProgramHeader64 elfProgramHeader = reader.Read<ElfProgramHeader64>(headerPositon);
			_attributes = (ElfProgramHeaderAttributes)elfProgramHeader.Flags;
			Type = elfProgramHeader.Type;
			VirtualAddress = (long)elfProgramHeader.VirtualAddress;
			VirtualSize = (long)elfProgramHeader.VirtualSize;
			FileOffset = (long)elfProgramHeader.FileOffset;
			FileSize = (long)elfProgramHeader.FileSize;
		}
		else
		{
			ElfProgramHeader32 elfProgramHeader2 = reader.Read<ElfProgramHeader32>(headerPositon);
			_attributes = (ElfProgramHeaderAttributes)elfProgramHeader2.Flags;
			Type = elfProgramHeader2.Type;
			VirtualAddress = elfProgramHeader2.VirtualAddress;
			VirtualSize = elfProgramHeader2.VirtualSize;
			FileOffset = elfProgramHeader2.FileOffset;
			FileSize = elfProgramHeader2.FileSize;
		}
		if (isVirtual && Type == ElfProgramHeaderType.Load)
		{
			AddressSpace = new RelativeAddressSpace(reader.DataSource, "ProgramHeader", VirtualAddress, VirtualSize);
		}
		else
		{
			AddressSpace = new RelativeAddressSpace(reader.DataSource, "ProgramHeader", fileOffset + FileOffset, FileSize);
		}
	}
}
