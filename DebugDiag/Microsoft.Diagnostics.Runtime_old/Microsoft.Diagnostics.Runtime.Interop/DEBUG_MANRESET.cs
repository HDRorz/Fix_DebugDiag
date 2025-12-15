using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_MANRESET : uint
{
	DEFAULT = 0u,
	LOAD_DLL = 1u
}
