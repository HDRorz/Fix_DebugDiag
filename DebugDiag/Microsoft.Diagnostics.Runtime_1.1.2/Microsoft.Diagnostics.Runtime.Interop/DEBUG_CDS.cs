using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CDS : uint
{
	ALL = uint.MaxValue,
	REGISTERS = 1u,
	DATA = 2u,
	REFRESH = 4u
}
