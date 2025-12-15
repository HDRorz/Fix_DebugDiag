using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_OUTSYM : uint
{
	DEFAULT = 0u,
	FORCE_OFFSET = 1u,
	SOURCE_LINE = 2u,
	ALLOW_DISPLACEMENT = 4u
}
