using System;

namespace Microsoft.Diagnostics.Runtime.Linux;

[Flags]
internal enum ElfProgramHeaderAttributes : uint
{
	Executable = 1u,
	Writable = 2u,
	Readable = 4u,
	OSMask = 0xFF00000u,
	ProcessorMask = 0xF0000000u
}
