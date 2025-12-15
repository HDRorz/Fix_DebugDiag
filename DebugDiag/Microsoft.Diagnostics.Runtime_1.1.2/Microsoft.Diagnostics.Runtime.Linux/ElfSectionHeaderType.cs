namespace Microsoft.Diagnostics.Runtime.Linux;

internal enum ElfSectionHeaderType : uint
{
	Null = 0u,
	ProgBits = 1u,
	SymTab = 2u,
	StrTab = 3u,
	Rela = 4u,
	Hash = 5u,
	Dynamic = 6u,
	Note = 7u,
	NoBits = 8u,
	Rel = 9u,
	ShLib = 10u,
	DynSym = 11u,
	InitArray = 14u,
	FiniArray = 15u,
	PreInitArray = 16u,
	Group = 17u,
	SymTabIndexes = 18u,
	Num = 19u,
	GnuAttributes = 1879048181u,
	GnuHash = 1879048182u,
	GnuLibList = 1879048183u,
	CheckSum = 1879048184u,
	GnuVerDef = 1879048189u,
	GnuVerNeed = 1879048190u,
	GnuVerSym = 1879048191u
}
