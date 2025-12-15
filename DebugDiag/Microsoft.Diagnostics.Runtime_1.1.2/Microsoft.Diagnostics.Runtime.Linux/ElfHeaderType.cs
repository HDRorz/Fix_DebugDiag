namespace Microsoft.Diagnostics.Runtime.Linux;

internal enum ElfHeaderType : ushort
{
	Relocatable = 1,
	Executable,
	Shared,
	Core
}
