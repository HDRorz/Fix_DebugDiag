using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_MANAGED : uint
{
	DISABLED = 0u,
	ALLOWED = 1u,
	DLL_LOADED = 2u
}
