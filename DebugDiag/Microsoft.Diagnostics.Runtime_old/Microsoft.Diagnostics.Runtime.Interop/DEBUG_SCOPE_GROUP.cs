using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_SCOPE_GROUP : uint
{
	ARGUMENTS = 1u,
	LOCALS = 2u,
	ALL = 3u
}
