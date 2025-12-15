using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_FRAME : uint
{
	DEFAULT = 0u,
	IGNORE_INLINE = 1u
}
