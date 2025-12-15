using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_REGISTERS : uint
{
	DEFAULT = 0u,
	INT32 = 1u,
	INT64 = 2u,
	FLOAT = 4u,
	ALL = 7u
}
