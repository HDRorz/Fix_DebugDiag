using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_MANRESET : uint
{
	DEFAULT = 0u,
	LOAD_DLL = 1u
}
