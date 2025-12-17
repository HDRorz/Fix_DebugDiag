using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.AnalysisRules.inc;

public class WCFRequestItem
{
	public ClrObject OperationContext { get; set; }

	public string Endpoint { get; set; }

	public string ContractType { get; set; }

	public string RequestState { get; set; }

	public string HttpContext { get; set; }

	public string OperationTimeout { get; set; }

	public string ChannelState { get; set; }

	public string ReplySent { get; set; }

	public string Aborted { get; set; }

	public string ThreadID { get; set; }

	public string MethodName { get; set; }

	public double CallDuration { get; set; }
}
