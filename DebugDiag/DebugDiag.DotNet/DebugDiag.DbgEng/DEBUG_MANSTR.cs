using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_MANSTR : uint
{
	NONE = 0u,
	LOADED_SUPPORT_DLL = 1u,
	LOAD_STATUS = 2u
}
