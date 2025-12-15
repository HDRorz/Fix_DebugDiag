using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_SYMBOL : uint
{
	EXPANSION_LEVEL_MASK = 0xFu,
	EXPANDED = 0x10u,
	READ_ONLY = 0x20u,
	IS_ARRAY = 0x40u,
	IS_FLOAT = 0x80u,
	IS_ARGUMENT = 0x100u,
	IS_LOCAL = 0x200u
}
