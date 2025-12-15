using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_GETMOD : uint
{
	DEFAULT = 0u,
	NO_LOADED_MODULES = 1u,
	NO_UNLOADED_MODULES = 2u
}
