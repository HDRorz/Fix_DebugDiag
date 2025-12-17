namespace DebugDiag.AnalysisRules.inc;

public class WCFServiceThrottle
{
	public int Count { get; set; }

	public int Max { get; set; }

	public int Queued { get; set; }

	public double Usage { get; set; }

	public WCFServiceThrottle(int count, int max, int queued)
	{
		Count = count;
		Max = max;
		Queued = queued;
		Usage = (double)count / (double)max;
	}

	public override string ToString()
	{
		return $"{Count}/{Max}/{Queued}";
	}
}
