using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_GSEL : uint
{
	DEFAULT = 0u,
	NO_SYMBOL_LOADS = 1u,
	ALLOW_LOWER = 2u,
	ALLOW_HIGHER = 4u,
	NEAREST_ONLY = 8u,
	INLINE_CALLSITE = 0x10u
}
