using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_TBINFO : uint
{
	NONE = 0u,
	EXIT_STATUS = 1u,
	PRIORITY_CLASS = 2u,
	PRIORITY = 4u,
	TIMES = 8u,
	START_OFFSET = 0x10u,
	AFFINITY = 0x20u,
	ALL = 0x3Fu
}
