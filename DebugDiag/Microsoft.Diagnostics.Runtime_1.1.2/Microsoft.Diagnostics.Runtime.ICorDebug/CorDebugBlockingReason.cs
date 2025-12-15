namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public enum CorDebugBlockingReason
{
	None,
	MonitorCriticalSection,
	MonitorEvent
}
