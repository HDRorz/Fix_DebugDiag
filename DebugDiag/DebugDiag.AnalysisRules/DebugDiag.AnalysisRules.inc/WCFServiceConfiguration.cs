using System.Collections.Generic;

namespace DebugDiag.AnalysisRules.inc;

public class WCFServiceConfiguration
{
	public TimeoutSettings ServiceTimeout { get; set; }

	public Dictionary<string, string> Behaviors { get; set; }

	public List<string> BaseAddress { get; set; }

	public List<WCFServiceChannel> Channels { get; set; }

	public WCFServiceConfiguration()
	{
		ServiceTimeout = new TimeoutSettings();
		Behaviors = new Dictionary<string, string>();
		BaseAddress = new List<string>();
		Channels = new List<WCFServiceChannel>();
	}
}
