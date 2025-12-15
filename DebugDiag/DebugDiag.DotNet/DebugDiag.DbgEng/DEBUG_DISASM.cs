using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_DISASM : uint
{
	EFFECTIVE_ADDRESS = 1u,
	MATCHING_SYMBOLS = 2u,
	SOURCE_LINE_NUMBER = 4u,
	SOURCE_FILE_NAME = 8u
}
