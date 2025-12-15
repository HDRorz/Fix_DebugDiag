using System;

namespace DebugDiag.DotNet.x86Analysis;

internal class AnalysisTimeoutException : Exception
{
	public TimeSpan Timeout;

	public AnalysisTimeoutException(TimeSpan timeout)
		: base("Analysis timed out.  Timeout = " + timeout.ToString())
	{
		Timeout = timeout;
	}
}
