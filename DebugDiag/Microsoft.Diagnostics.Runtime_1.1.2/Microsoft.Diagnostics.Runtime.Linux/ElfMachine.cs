namespace Microsoft.Diagnostics.Runtime.Linux;

internal enum ElfMachine : ushort
{
	EM_NONE = 0,
	EM_386 = 3,
	EM_PARISC = 15,
	EM_SPARC32PLUS = 18,
	EM_PPC = 20,
	EM_PPC64 = 21,
	EM_SPU = 23,
	EM_ARM = 40,
	EM_SH = 42,
	EM_SPARCV9 = 43,
	EM_IA_64 = 50,
	EM_X86_64 = 62,
	EM_S390 = 22,
	EM_CRIS = 76,
	EM_V850 = 87,
	EM_M32R = 88,
	EM_H8_300 = 46,
	EM_MN10300 = 89,
	EM_BLACKFIN = 106,
	EM_AARCH64 = 183,
	EM_FRV = 21569,
	EM_AVR32 = 6317
}
