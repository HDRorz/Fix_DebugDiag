using DebugDiag.DotNet;
using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.AnalysisRules.inc;

public class OperationContextPerThread
{
	public ClrObject StackObject { get; set; }

	public NetDbgThread Thread { get; set; }

	public OperationContextPerThread(NetDbgThread thread, ClrObject stackObject)
	{
		StackObject = stackObject;
		Thread = thread;
	}
}
