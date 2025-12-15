namespace Microsoft.Diagnostics.Runtime.Interop;

public enum DEBUG_STATUS_FLAGS : ulong
{
	INSIDE_WAIT = 0x100000000uL,
	WAIT_TIMEOUT = 0x200000000uL
}
