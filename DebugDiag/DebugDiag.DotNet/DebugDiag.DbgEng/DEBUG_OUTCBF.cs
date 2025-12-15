using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_OUTCBF : uint
{
	EXPLICIT_FLUSH = 1u,
	DML_HAS_TAGS = 2u,
	DML_HAS_SPECIAL_CHARACTERS = 4u
}
