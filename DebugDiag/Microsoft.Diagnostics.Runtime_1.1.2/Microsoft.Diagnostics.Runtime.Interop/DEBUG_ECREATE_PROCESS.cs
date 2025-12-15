using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_ECREATE_PROCESS : uint
{
	DEFAULT = 0u,
	INHERIT_HANDLES = 1u,
	USE_VERIFIER_FLAGS = 2u,
	USE_IMPLICIT_COMMAND_LINE = 4u
}
