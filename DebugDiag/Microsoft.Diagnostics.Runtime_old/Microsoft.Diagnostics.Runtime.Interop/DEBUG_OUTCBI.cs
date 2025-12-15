using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_OUTCBI : uint
{
	EXPLICIT_FLUSH = 1u,
	TEXT = 2u,
	DML = 4u,
	ANY_FORMAT = 6u
}
