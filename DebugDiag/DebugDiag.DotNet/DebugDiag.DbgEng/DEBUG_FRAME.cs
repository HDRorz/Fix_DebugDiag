using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_FRAME : uint
{
	DEFAULT = 0u,
	IGNORE_INLINE = 1u
}
