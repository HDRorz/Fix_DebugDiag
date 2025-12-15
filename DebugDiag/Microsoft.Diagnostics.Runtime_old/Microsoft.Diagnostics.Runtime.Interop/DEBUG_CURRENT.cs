using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CURRENT : uint
{
	DEFAULT = 0xFu,
	SYMBOL = 1u,
	DISASM = 2u,
	REGISTERS = 4u,
	SOURCE_LINE = 8u
}
