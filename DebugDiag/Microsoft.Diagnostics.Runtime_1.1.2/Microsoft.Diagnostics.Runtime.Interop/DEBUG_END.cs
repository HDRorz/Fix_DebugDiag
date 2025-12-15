namespace Microsoft.Diagnostics.Runtime.Interop;

public enum DEBUG_END : uint
{
	PASSIVE,
	ACTIVE_TERMINATE,
	ACTIVE_DETACH,
	END_REENTRANT,
	END_DISCONNECT
}
