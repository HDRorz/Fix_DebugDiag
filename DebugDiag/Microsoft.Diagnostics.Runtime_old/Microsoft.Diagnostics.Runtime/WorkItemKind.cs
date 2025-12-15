namespace Microsoft.Diagnostics.Runtime;

public enum WorkItemKind
{
	Unknown,
	AsyncTimer,
	AsyncCallback,
	QueueUserWorkItem,
	TimerDelete
}
