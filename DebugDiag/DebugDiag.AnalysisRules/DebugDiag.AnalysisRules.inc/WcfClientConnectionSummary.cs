namespace DebugDiag.AnalysisRules.inc;

public class WcfClientConnectionSummary
{
	public string ConnectionPoolName { get; set; }

	public int CurrentConnection { get; set; }

	public string Endpoint { get; set; }

	public int MaxConnection { get; set; }

	public int WaitList { get; set; }
}
