namespace DebugDiag.AnalysisRules.inc;

public class DebugDiagReportIssue
{
	public string Type { get; set; }

	public string Description { get; set; }

	public string Recommendation { get; set; }

	public int Weight { get; set; }

	public DebugDiagReportIssue(string type, string description, string recommendation, int weight = 0)
	{
		Type = type;
		Description = description;
		Recommendation = recommendation;
		Weight = weight;
	}
}
