using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_PROCESS : uint
{
	DEFAULT = 0u,
	DETACH_ON_EXIT = 1u,
	ONLY_THIS_PROCESS = 2u
}
