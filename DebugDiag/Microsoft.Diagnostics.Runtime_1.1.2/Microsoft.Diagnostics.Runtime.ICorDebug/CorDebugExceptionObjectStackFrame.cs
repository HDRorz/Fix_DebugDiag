namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct CorDebugExceptionObjectStackFrame
{
	public ICorDebugModule pModule;

	public ulong ip;

	public int methodDef;

	public bool isLastForeignException;
}
