using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_GETMOD : uint
{
	DEFAULT = 0u,
	NO_LOADED_MODULES = 1u,
	NO_UNLOADED_MODULES = 2u
}
