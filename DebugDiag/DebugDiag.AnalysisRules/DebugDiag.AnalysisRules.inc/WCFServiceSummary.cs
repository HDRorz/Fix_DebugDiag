namespace DebugDiag.AnalysisRules.inc;

public class WCFServiceSummary
{
	public string ServiceType { get; set; }

	public string ServiceTypeHost { get; set; }

	public string TypeLoaded { get; set; }

	public string IsThrottled { get; set; }

	public string State { get; set; }

	public string InstanceMode { get; set; }

	public string ConcurrencyMode { get; set; }

	public WCFServiceConfiguration ServiceConfiguration { get; set; }

	public WCFServiceThrottle Session { get; set; }

	public WCFServiceThrottle Call { get; set; }

	public WCFServiceThrottle Instance { get; set; }

	public WCFServiceSummary(string contractName, string serviceType, string isThrottled, string state, WCFServiceThrottle sessionThrottling, WCFServiceThrottle callThrottling, WCFServiceThrottle instanceThrottling, string instaceMode, string concurrencyMode, WCFServiceConfiguration serviceConfiguration)
	{
		ServiceTypeHost = serviceType;
		ServiceType = contractName;
		State = state;
		IsThrottled = isThrottled;
		InstanceMode = instaceMode;
		ConcurrencyMode = concurrencyMode;
		ServiceConfiguration = serviceConfiguration;
		Session = sessionThrottling;
		Call = callThrottling;
		Instance = instanceThrottling;
	}
}
