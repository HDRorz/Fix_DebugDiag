using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_FIND_SOURCE : uint
{
	DEFAULT = 0u,
	FULL_PATH = 1u,
	BEST_MATCH = 2u,
	NO_SRCSRV = 4u,
	TOKEN_LOOKUP = 8u
}
