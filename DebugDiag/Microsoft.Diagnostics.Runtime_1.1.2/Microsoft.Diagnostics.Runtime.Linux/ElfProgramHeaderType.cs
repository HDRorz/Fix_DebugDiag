namespace Microsoft.Diagnostics.Runtime.Linux;

internal enum ElfProgramHeaderType : uint
{
	Null,
	Load,
	Dynamic,
	Interp,
	Note,
	Shlib,
	Phdr
}
