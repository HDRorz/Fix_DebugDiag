using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_LOG : uint
{
	DEFAULT = 0u,
	APPEND = 1u,
	UNICODE = 2u,
	DML = 4u
}
