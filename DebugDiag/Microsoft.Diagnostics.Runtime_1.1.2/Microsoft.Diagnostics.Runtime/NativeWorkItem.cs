namespace Microsoft.Diagnostics.Runtime;

public abstract class NativeWorkItem
{
	public abstract WorkItemKind Kind { get; }

	public abstract ulong Callback { get; }

	public abstract ulong Data { get; }
}
