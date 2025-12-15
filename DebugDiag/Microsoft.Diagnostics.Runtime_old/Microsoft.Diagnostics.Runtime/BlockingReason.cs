namespace Microsoft.Diagnostics.Runtime;

public enum BlockingReason
{
	None,
	Unknown,
	Monitor,
	MonitorWait,
	WaitOne,
	WaitAll,
	WaitAny,
	ThreadJoin,
	ReaderAcquired,
	WriterAcquired
}
