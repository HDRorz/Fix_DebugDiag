using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_OUTPUT_SYMBOLS
{
	DEFAULT = 0,
	NO_NAMES = 1,
	NO_OFFSETS = 2,
	NO_VALUES = 4,
	NO_TYPES = 0x10
}
