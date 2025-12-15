using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CSS : uint
{
	ALL = uint.MaxValue,
	LOADS = 1u,
	UNLOADS = 2u,
	SCOPE = 4u,
	PATHS = 8u,
	SYMBOL_OPTIONS = 0x10u,
	TYPE_OPTIONS = 0x20u
}
