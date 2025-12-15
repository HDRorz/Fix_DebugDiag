using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CREATE_PROCESS : uint
{
	DEFAULT = 0u,
	NO_DEBUG_HEAP = 0x400u,
	THROUGH_RTL = 0x10000u
}
