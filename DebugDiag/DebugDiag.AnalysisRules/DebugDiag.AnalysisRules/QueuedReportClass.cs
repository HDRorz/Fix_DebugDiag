using System.Text;

namespace DebugDiag.AnalysisRules;

public class QueuedReportClass
{
	public StringBuilder Details;

	public StringBuilder Summary;

	public StringBuilder TableOfContents;

	public bool Visible;

	public string UniqueKey;

	public QueuedReportClass()
	{
		Initialize();
	}

	private void Initialize()
	{
		Details = new StringBuilder();
		Summary = new StringBuilder();
		TableOfContents = new StringBuilder();
		Visible = true;
	}
}
