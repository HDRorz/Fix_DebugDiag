using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_GET_PROC : uint
{
	DEFAULT = 0u,
	FULL_MATCH = 1u,
	ONLY_MATCH = 2u,
	SERVICE_NAME = 4u
}
