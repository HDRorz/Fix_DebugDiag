using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_BREAKPOINT_ACCESS_TYPE : uint
{
	READ = 1u,
	WRITE = 2u,
	EXECUTE = 4u,
	IO = 8u
}
