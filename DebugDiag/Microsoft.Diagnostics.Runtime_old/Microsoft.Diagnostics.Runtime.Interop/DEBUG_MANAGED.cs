using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_MANAGED : uint
{
	DISABLED = 0u,
	ALLOWED = 1u,
	DLL_LOADED = 2u
}
