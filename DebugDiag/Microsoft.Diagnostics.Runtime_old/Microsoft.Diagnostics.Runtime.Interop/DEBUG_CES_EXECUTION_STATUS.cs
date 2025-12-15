using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CES_EXECUTION_STATUS : ulong
{
	INSIDE_WAIT = 0x100000000uL,
	WAIT_TIMEOUT = 0x200000000uL
}
