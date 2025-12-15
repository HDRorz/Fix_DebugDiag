using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_BREAKPOINT_FLAG : uint
{
	GO_ONLY = 1u,
	DEFERRED = 2u,
	ENABLED = 4u,
	ADDER_ONLY = 8u,
	ONE_SHOT = 0x10u
}
