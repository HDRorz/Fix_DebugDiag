using System;
using System.Collections.Generic;

namespace DebugDiag.AnalysisRules.inc;

public class TimeoutSettings
{
	public Dictionary<string, TimeSpan> Timeouts = new Dictionary<string, TimeSpan>();
}
