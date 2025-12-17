using System.Collections.Generic;
using System.Text;
using DebugDiag.DotNet;

namespace DebugDiag.AnalysisRules;

public class QueuedReportsClass
{
	private NetScriptManager Manager = Globals.Manager;

	private Dictionary<string, QueuedReportGroupClass> m_MessageGroups;

	public QueuedReportsClass()
	{
		Initialize();
	}

	private void Initialize()
	{
		m_MessageGroups = new Dictionary<string, QueuedReportGroupClass>();
	}

	public QueuedReportGroupClass GetGroup(string groupID)
	{
		if (m_MessageGroups.ContainsKey(groupID))
		{
			return m_MessageGroups[groupID];
		}
		QueuedReportGroupClass queuedReportGroupClass = new QueuedReportGroupClass();
		m_MessageGroups.Add(groupID, queuedReportGroupClass);
		return queuedReportGroupClass;
	}

	public void WriteSummaries()
	{
		foreach (QueuedReportGroupClass value in m_MessageGroups.Values)
		{
			if (value.Consolidated)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(value.Message + "<br>");
				foreach (string key in value.Items.Keys)
				{
					stringBuilder.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
					stringBuilder.AppendLine(key + "<br>");
				}
				stringBuilder.Append("<br>");
				Globals.Manager.ReportOther(stringBuilder.ToString(), "", "Notification", "notificationicon.png", 0, "{c21ef9f9-6663-4421-96f4-6a7fc8f2f47b}");
				continue;
			}
			foreach (QueuedReportClass value2 in value.Items.Values)
			{
				if (value2.Visible)
				{
					Globals.Manager.ReportOther(value2.Summary.ToString(), "", "Notification", "notificationicon.png", 0, "{fee0451b-88e0-40c5-b425-b89ef39ce396}");
				}
			}
		}
	}

	public void WriteDetails()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(HelperFunctionsImpl.vbCrLf + HelperFunctionsImpl.vbCrLf);
		stringBuilder.AppendLine("<script language='JavaScript'>");
		stringBuilder.AppendLine("AddToggler('doToggleCrashHangAnalysis');");
		stringBuilder.AppendLine("function doToggleCrashHangAnalysis()");
		stringBuilder.AppendLine("{");
		foreach (QueuedReportGroupClass value in m_MessageGroups.Values)
		{
			foreach (string key in value.Items.Keys)
			{
				if (!value.Items[key].Visible)
				{
					stringBuilder.Append("  doToggle2(document.all('");
					stringBuilder.Append(value.Items[key].UniqueKey);
					stringBuilder.AppendLine("-t'));");
				}
			}
		}
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine("</script>");
		stringBuilder.Append(HelperFunctionsImpl.vbCrLf);
		Globals.Manager.Write(stringBuilder.ToString());
	}
}
