namespace DebugDiag.AnalysisRules.inc;

public class WCFSMSvchostRoute
{
	public string Endpoint { get; set; }

	public int SessionWorker { get; set; }

	public int SessionMessageQueue { get; set; }

	public int QueueSize { get; set; }

	public string State { get; set; }

	public string WebSite { get; set; }

	public string WebAppPool { get; set; }

	public string[] Process { get; set; }

	public WCFSMSvchostRoute(string endpoint, int sessionWorker, int sessionMessageQueue, int queueSize, string state, string webSite, string webAppPool, string[] process)
	{
		Endpoint = endpoint;
		Process = process;
		QueueSize = queueSize;
		SessionMessageQueue = sessionMessageQueue;
		SessionWorker = sessionWorker;
		WebAppPool = webAppPool;
		WebSite = webSite;
		State = state;
	}
}
