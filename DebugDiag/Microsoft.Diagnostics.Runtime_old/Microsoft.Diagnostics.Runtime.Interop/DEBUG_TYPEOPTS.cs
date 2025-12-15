using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_TYPEOPTS : uint
{
	UNICODE_DISPLAY = 1u,
	LONGSTATUS_DISPLAY = 2u,
	FORCERADIX_OUTPUT = 4u,
	MATCH_MAXSIZE = 8u
}
