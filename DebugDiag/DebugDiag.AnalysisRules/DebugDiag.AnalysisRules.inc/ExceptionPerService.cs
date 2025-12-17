using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.AnalysisRules.inc;

public class ExceptionPerService
{
	public string ServiceContract { get; set; }

	public int ExceptionCount { get; set; }

	public IList<ClrException> ExceptionList { get; set; }
}
