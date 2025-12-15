namespace Microsoft.Diagnostics.Runtime.Linux;

internal interface IElfHeader
{
	bool Is64Bit { get; }

	bool IsValid { get; }

	ElfHeaderType Type { get; }

	ElfMachine Architecture { get; }

	long ProgramHeaderOffset { get; }

	long SectionHeaderOffset { get; }

	ushort ProgramHeaderEntrySize { get; }

	ushort ProgramHeaderCount { get; }

	ushort SectionHeaderEntrySize { get; }

	ushort SectionHeaderCount { get; }

	ushort SectionHeaderStringIndex { get; }
}
