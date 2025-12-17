namespace DebugDiag.AnalysisRules.inc;

public class WCFServiceChannel
{
	public string Name { get; set; }

	public string ListenURI { get; set; }

	public string State { get; set; }

	public string SessionMode { get; set; }

	public string ReliableSession { get; set; }

	public string ReliableSessionInactivityTimeout { get; set; }

	public TimeoutSettings channelTimeout { get; set; }
}
