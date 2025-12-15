using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_EXECUTE : uint
{
	DEFAULT = 0u,
	ECHO = 1u,
	NOT_LOGGED = 2u,
	NO_REPEAT = 4u
}
