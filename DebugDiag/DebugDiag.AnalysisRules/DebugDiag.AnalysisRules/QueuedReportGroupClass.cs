using System.Collections.Generic;

namespace DebugDiag.AnalysisRules;

public class QueuedReportGroupClass
{
	public bool Consolidated;

	public string Message;

	public Dictionary<string, QueuedReportClass> Items;

	public QueuedReportGroupClass()
	{
		Initialize();
	}

	private void Initialize()
	{
		Consolidated = false;
		Message = "";
		Items = new Dictionary<string, QueuedReportClass>();
	}

	public QueuedReportClass GetReport(string dumpName)
	{
		if (Items.ContainsKey(dumpName))
		{
			return Items[dumpName];
		}
		QueuedReportClass queuedReportClass = new QueuedReportClass();
		Items.Add(dumpName, queuedReportClass);
		return queuedReportClass;
	}
}
