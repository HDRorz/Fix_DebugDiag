using System;
using System.Collections.Generic;
using System.Text;
using DebugDiag.DotNet.Reports;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public class PerfFunctions
{
	public static void BuildPerfReportTOC()
	{
		Globals.Manager.Write("<h4>Table Of Contents</h4>");
		AddTOCLink(0, "Input Summary");
		AddTOCLink(0, "Performance Summary");
		AddTOCLink(1, "Statistical Rollups");
		AddTOCLink(0, "Performance Details");
		AddTOCLink(0, "Report Legend");
		Globals.Manager.Write("<br><br>");
	}

	public static void AddTOCLink(int indent, string title)
	{
		Globals.Manager.Write(Globals.HelperFunctions.Spaces(indent * 5) + "<a href='#" + Globals.HelperFunctions.GetCanonacolizedLinkKey(title) + "-t'><b>" + title + "</b></a><br>");
	}

	public static void ReportAllOperations()
	{
		ReportDumpSummary();
		ReportLegend();
		ReportPerfSummary();
		ReportAllOperationDetails();
	}

	public static void ReportLegend()
	{
		ReportSection val = Globals.Manager.AddReportSection("REPORT_LEGEND", (SectionType)0);
		val.Title = "Report Legend";
		val.Collapsible = true;
		val.Collapsed = true;
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("<TABLE class=\"myCustomText\" id=\"Table4\" cellSpacing=\"1\" cellPadding=\"1\" width=\"1000\" border=\"0\">\r\n\t<TR>\r\n\t\t<TH vAlign=\"middle\" align=\"center\" height=\"50\">\r\n\t\t\t<U><a name='Abstract'>Abstract</a></U></TH></TR>\r\n\t<TR>\r\n\t\t<TD>\r\n\t\t\t<P>This rule is designed to assist in the troubleshooting of performance issues \r\n\t\t\t\tby&nbsp;quickly narrowing the focus of investigation to the particular \r\n\t\t\t\toperation(s), threads(s), and/or function(s) of interest, which would otherwise \r\n\t\t\t\tbe an extremely tedious task - particularly when manually reviewing&nbsp;a large \r\n\t\t\t\tnumber of dump files of a busy multithreaded application.<br />\r\n\t\t\t\t<br />\r\n\t\t\t\tIt analyzes multiple \r\n\t\t\t\tdump files of a single process collected during the problem and provides \r\n\t\t\t\tprofiler-style function statistcs by treating each dump as&nbsp;a data sample - \r\n\t\t\t\tso the more data samples available, the more accurate the results will be.<br />\r\n\t\t\t\t<br />\r\n\t\t\t\tStatistics are provided for the entire process as well as for various subsets or \"rollups\" \r\n\t\t\t\t(i.e. stats for only the top 5 threads sorted by cpu consumption, or only the top 5 ASP \r\n\t\t\t\trequests sorted by duration, etc.).&nbsp; Typically the statistics for a \r\n\t\t\t\tparticular subset, rather than the entire process,&nbsp;are more helpful for \r\n\t\t\t\tidentifying the root of the performance issue, since typically much of the \r\n\t\t\t\tprocess remains in a healthy state while only the subset contributes to the \r\n\t\t\t\tperformance problem.<br />\r\n\t\t\t\t<br />\r\n\t\t\t\tIn later releases, this rule may seek to identify particular known issues.\r\n\t\t\t\tIn this release, the rule simply provides the user with the statistics to\r\n\t\t\t\tfacilitate a faster review of multiple dump files.\r\n\t\t\t</P>\r\n\t\t</TD>\r\n\t</TR>\r\n</TABLE>\r\n<BR>\r\n<TABLE class=\"myCustomText\" id=\"Table3\" cellSpacing=\"1\" cellPadding=\"1\" width=\"1000\" border=\"0\">\r\n\t<TR>\r\n\t\t<TH vAlign=\"middle\" align=\"center\" colSpan=\"2\" height=\"50\">\r\n\t\t\t<u>Glossary</u></TH></TR>\r\n\t<TR>\r\n\t\t<TH>\r\n\t\t\tTerm</TH>\r\n\t\t<TH>\r\n\t\t\tDescription</TH></TR>\r\n\t<TR>\r\n\t\t<TD noWrap>Operation&nbsp;&nbsp;&nbsp;</TD>\r\n\t\t<TD>A single logical unit of work performed by an application - for \r\n\t\t\texample,&nbsp;the execution of an&nbsp;ASP page (and any components invoked by \r\n\t\t\tthe page).&nbsp; A logical operation may span across multiple threads and \r\n\t\t\tprocesses, though in the simple case it is executed on a single thread.</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap>Hits</TD>\r\n\t\t<TD>The number of times a particular function appears within a given scope.  For example the scope might be a particular thread, a rollup of several high CPU threads, or all threads in the process.</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap></TD>\r\n\t\t<TD></TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap></TD>\r\n\t\t<TD></TD>\r\n\t</TR>\r\n</TABLE>\r\n<BR>\r\n<TABLE class=\"myCustomText\" id=\"Table2\" cellSpacing=\"1\" cellPadding=\"1\" width=\"1000\" border=\"0\">\r\n\t<TR>\r\n\t\t<TH vAlign=\"middle\" align=\"center\" colSpan=\"2\" height=\"50\">\r\n\t\t\t<u>Operation Types</u></TH></TR>\r\n\t<TR>\r\n\t\t<TH>\r\n\t\t\tType</TH>\r\n\t\t<TH>\r\n\t\t\tDescription</TH></TR>\r\n\t<TR>\r\n\t\t<TD noWrap>ASP</TD>\r\n\t\t<TD>An incoming ASP request</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap>ASP.NET</TD>\r\n\t\t<TD>An incoming ASP.NET request (includes WebServices)</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap>COM</TD>\r\n\t\t<TD>An incoming COM call (includes COM+)</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap>Unknown</TD>\r\n\t\t<TD>None of the above (i.e. custom code)</TD>\r\n\t</TR>\r\n</TABLE>\r\n<BR>\r\n<TABLE class=\"myCustomText\" id=\"Table1\" cellSpacing=\"1\" cellPadding=\"1\" width=\"1000\" border=\"0\">\r\n\t<TR>\r\n\t\t<TH height=\"50\" valign=\"middle\" align=\"center\" colspan=\"3\">\r\n\t\t\t<u>Stack Frame Notation</u></TH>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TH>\r\n\t\t\tMarkup</TH>\r\n\t\t<TH>\r\n\t\t\tFrame Type</TH>\r\n\t\t<TH>\r\n\t\t\tDescription</TH></TR>\r\n\t<TR>\r\n\t\t<TD noWrap><FONT color=\"blue\">blue</FONT></TD>\r\n\t\t<TD noWrap>User Function</TD>\r\n\t\t<TD>Functions in modules not belonging to the system (i.e. 3rd party)</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD noWrap><FONT color=\"blue\"><EM>blue italic</EM></FONT></TD>\r\n\t\t<TD noWrap>Top User Function - Local</TD>\r\n\t\t<TD>One of the most frequent user functions in the particular operation or set of operations</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD NOWRAP><STRONG><FONT color=\"red\">red bold</FONT></STRONG></TD>\r\n\t\t<TD NOWRAP>Top User Function - Global</TD>\r\n\t\t<TD>One of the most frequent user functions in all operations</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD NOWRAP><FONT color=\"red\"><STRONG><EM>red bold italic</EM></STRONG></FONT></TD>\r\n\t\t<TD NOWRAP>Top User Function - Both</TD>\r\n\t\t<TD>One of the most frequent user functions in both the particular operation(s), as \r\n\t\t\twell as in all operations</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD NOWRAP><FONT color=\"#777777\">dark gray</FONT></TD>\r\n\t\t<TD NOWRAP>System Function</TD>\r\n\t\t<TD>Functions in modules belonging to the system</TD>\r\n\t</TR>\r\n\t<TR>\r\n\t\t<TD NOWRAP><FONT color=\"#CCCCCC\">light gray</FONT></TD>\r\n\t\t<TD NOWRAP>Boilerplate Function</TD>\r\n\t\t<TD>Extremely common system functions which can generally be safely ignored</TD>\r\n\t</TR>\r\n</TABLE>\r\n<BR>\r\n");
		Globals.Manager.CurrentSection = val.Parent;
	}

	public static void ReportDumpSummary()
	{
		double time = 0.0;
		double num = 0.0;
		int num2 = 0;
		CDump cDump = null;
		Globals.HelperFunctions.ResetStatus("Reporting Input Summary", Globals.g_AllOperations.Count, "Dump");
		ReportSection val = Globals.Manager.AddReportSection("INPUT_SUMMARY", (SectionType)0);
		val.Title = "Input Summary";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
		Globals.Manager.Write("<th nowrap style='padding-right:20px'><u>Alias</u></th><th nowrap style='padding-right:20px'><u>File Creation Time</u></th><th nowrap style='padding-right:20px'><u>Process Up-Time</u></th><th nowrap style='padding-right:20px'><u>Relative Time</u></th><th nowrap style='padding-right:20px'><u>Dump File Name</u></th>");
		for (num2 = 1; num2 <= Globals.g_dumps.Count; num2++)
		{
			Globals.HelperFunctions.IncrementSubStatus();
			cDump = Globals.g_dumps.DumpBySortedDumpNumber(num2);
			num = cDump.ProcessUpTime;
			DateTime systemTime = cDump.SystemTime;
			cDump.CloseDebugger();
			if (time.Equals(0.0))
			{
				time = num;
			}
			Globals.Manager.Write("<tr><td nowrap style='padding-right:20px'><a name='DumpInfo_" + Convert.ToString(num2) + "'>\"Dump " + Convert.ToString(num2) + "\"</a></td><td nowrap style='padding-right:20px'>" + Convert.ToString(systemTime) + "</td><td nowrap style='padding-right:20px'>" + Globals.HelperFunctions.PrintTime(num) + "</td><td nowrap style='padding-right:20px'>" + PrintTimeDiff(time, num) + "</td><td nowrap style='padding-right:20px'>" + cDump.ShortFileName + "</td></tr>");
		}
		Globals.HelperFunctions.ClearSubStatus();
		Globals.Manager.Write("</table>");
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static void ReportPerfSummary()
	{
		string text = "";
		string text2 = "";
		Globals.Manager.ReportOther("If you are new to the PerformanceAnalysis rule, please read the <a href='#abstract'>abstract</a> for more information on how to interpret the results of this report. &nbsp;  Note that the abstract may be hidden if the <a href='#" + Globals.HelperFunctions.GetCanonacolizedLinkKey("Report Legend") + "-t'>Report Legend</a> section is collapsed.", "", "Notification", "notificationicon.png", 1, "{f46340ba-10d3-49a0-a60c-ab0c9d5145d1}");
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting() && !Globals.g_DoCombinedNativeMangedPerfAnalysis)
		{
			Globals.Manager.ReportOther("Managed (.NET) code was detected in one or more dump files, so analysis was only performed on managed (.NET) code.", "", "Notification", "notificationicon.png", 777, "{ce70eef0-ebb3-47b8-8db5-870d51c97ba4}");
		}
		Globals.HelperFunctions.ResetStatus("Reporting Performance Summary", Globals.g_AllOperations.Count, "Operation Group");
		ReportSection val = Globals.Manager.AddReportSection("PERF_SUMMARY", (SectionType)0);
		val.Title = "Performance Summary";
		val.Collapsible = true;
		ReportSection val2 = val.AddChildSection("STAT_ROLLUPS", (SectionType)0);
		val2.Title = "Statistical Rollups";
		val2.Collapsible = true;
		Globals.Manager.CurrentSection = val2;
		Globals.HelperFunctions.IncrementSubStatus();
		COperations cOperations = new COperations();
		cOperations.LoadTopXFromSortedList(Globals.g_AllOperations.OperationsByDuration, excludeUnknownOps: true, 4);
		text2 = GetTopOperationsName(cOperations, "Operations By Duration");
		cOperations.ShowStatsEx(text2 + Globals.HelperFunctions.Spaces(6) + "(excludes operations of 'unknown' type)", "Click on any operation to view the full call stacks in the details below", collapsed: false, 4);
		if (Convert.ToInt32(cOperations.Count) > 0)
		{
			text = "<a href='#" + 4 + val2.Parent.GetUID + "'>" + GetTopOperationsName(cOperations, "Operations By Duration") + "</a>";
			Globals.Manager.ReportWarning("Well known operations, such as ASP or ASP.NET requests were detected on one or more threads", "Review the details in the " + text + " section of the report.  This is a 'Statistical Rollup' which only includes the subset of statistics which belong to these well known operations (i.e. to the ASP.NET requests)", 888, "{46347bd5-3edc-4655-a1cc-bac65a26620e}");
		}
		text = "";
		Globals.HelperFunctions.IncrementSubStatus();
		cOperations = new COperations();
		cOperations.LoadTopXFromSortedList(Globals.g_AllOperations.OperationsByAvgCpu, excludeUnknownOps: false, 1);
		text2 = GetTopOperationsName(cOperations, "Threads By Avg CPU");
		cOperations.ShowStatsEx(text2, "Click on any operation in the list below to jump down to the full call stacks which belong to the operation.", collapsed: false, 1);
		if (Convert.ToInt32(cOperations.Count) > 0)
		{
			text = "<a href='#" + 1 + val2.Parent.GetUID + "'>" + GetTopOperationsName(cOperations, "Threads By Avg CPU") + "</a>";
			Globals.Manager.ReportWarning("High CPU usage between dump files was detected on one or more threads.", "Review the details in the " + text + " section of the report.  This is a 'Statistical Rollup' which only includes the subset of statistics which belong to the threads consuming the most CPU", 999, "{97391507-d736-4f1b-90e5-05572c03722c}");
		}
		Globals.HelperFunctions.IncrementSubStatus();
		cOperations = new COperations();
		cOperations.LoadTopXFromSortedList(Globals.g_AllOperations.OperationsByMaxCpu, excludeUnknownOps: false, 2);
		text2 = GetTopOperationsName(cOperations, "Threads By Max CPU");
		cOperations.ShowStatsEx(text2, "Click on any operation in the list below to jump down to the full call stacks which belong to the operation.", collapsed: false, 2);
		if (text == "" && Convert.ToInt32(cOperations.Count) > 0)
		{
			text = "<a href='#" + 2 + val2.Parent.GetUID + "'>" + GetTopOperationsName(cOperations, "Threads By Max CPU") + "</a>";
			Globals.Manager.ReportWarning("High CPU usage between dump files was detected on one or more threads.", "Review the details in the " + text + " section of the report.  This is a 'Statistical Rollup' which only includes the subset of statistics which belong to the threads consuming the most CPU", 999, "{6e5fdb6e-6c0f-4fcc-af11-c3e63558c42a}");
		}
		Globals.g_AllOperations.ShowStatsEx("All Operations", "To search on any particular function in the list below, highlight it with the mouse then press CTRL+C, CTRL+F, CTRL+V.  This will highlight all the call stacks which contain the particular function and allow you to jump between them in the browser.", collapsed: false, "OPSTATS_ALL");
		Globals.HelperFunctions.ClearSubStatus();
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static string GetTopOperationsName(COperations operations, string title)
	{
		return "Top " + Convert.ToString(operations.Count) + " " + title;
	}

	private static void ReportAllOperationDetails()
	{
		Globals.HelperFunctions.ResetStatus("Reporting " + Convert.ToString("Performance Details"), Globals.g_AllOperations.Count, "Operation");
		ReportSection val = Globals.Manager.AddReportSection("PERF_DETAILS", (SectionType)0);
		val.Title = "Performance Details";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("<BR><BR><font size=4 color='Green'><i><u>Tip</u>:&nbsp;&nbsp;</i></font>To get a mile-high view of all the dumps, and easily find the hot-spots, zoom out to 10% in Internet Explorer by using 'CTRL -' or the 'Change Zoom Level' button at the far right of the status bar.<BR><BR><BR>");
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
		foreach (string key in Globals.g_AllOperations.OperationsByDuration.Keys)
		{
			Globals.HelperFunctions.IncrementSubStatus();
			ReportOperationDetails(Globals.g_AllOperations.OperationsByDuration[key]);
		}
		Globals.HelperFunctions.ClearSubStatus();
		Globals.Manager.Write("</table>");
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static void ReportCallStack(Dictionary<int, CRelevantStackFrame> relevantStackFrames, COperation operation)
	{
		CRelevantStackFrame cRelevantStackFrame = null;
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><td>");
		foreach (int key in relevantStackFrames.Keys)
		{
			Globals.Manager.Write("<tr><td nowrap>");
			cRelevantStackFrame = relevantStackFrames[key];
			Globals.Manager.Write(FormatFunctionName(cRelevantStackFrame.FunctionNameNoOffset, bChop: false, operation));
			Globals.Manager.Write("</td></tr>");
		}
		Globals.Manager.Write("</td></tr></table><br><br>");
	}

	private static void ReportThreadInfo(CacheFunctions.ScriptThreadClass lastThread, CacheFunctions.ScriptThreadClass thread, bool showGeneral, bool showTimes)
	{
		int Days = 0;
		int Hours = 0;
		int Minutes = 0;
		int Seconds = 0;
		int MilliSeconds = 0;
		int num = 0;
		int Days2 = 0;
		int Hours2 = 0;
		int Minutes2 = 0;
		int Seconds2 = 0;
		int MilliSeconds2 = 0;
		int num2 = 0;
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
		if (showGeneral)
		{
			Globals.Manager.Write("<th><u>Thread Info</u></th>");
			if (Convert.ToUInt64(thread.StartAddress) != 0L)
			{
				Globals.Manager.Write("<tr><td nowrap>Entry point</td><td nowrap>&nbsp;&nbsp;" + thread.StartAddressSymbol + "</td></tr>");
			}
			Globals.Manager.Write("<tr><td nowrap>System ID</td><td  nowrap>&nbsp;&nbsp;" + Convert.ToString(thread.SystemID) + " (0x" + Convert.ToInt32(thread.SystemID).ToString("X") + ")</td></tr>");
			Globals.Manager.Write("<tr><td nowrap>Create time</td><td  nowrap>&nbsp;&nbsp;" + Convert.ToString(thread.CreateTime) + "</td></tr>");
		}
		if (Globals.g_ExtendedThreadInfoAvailable && showTimes)
		{
			thread.GetUserTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
			Globals.Manager.Write("<tr><td nowrap>User Time</td><td  nowrap>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintTime2(Days, Hours, Minutes, Seconds, MilliSeconds));
			if (lastThread != null)
			{
				lastThread.GetUserTime(out Days2, out Hours2, out Minutes2, out Seconds2, out MilliSeconds2);
				num = Days * 86400000 + Hours * 3600000 + Minutes * 60000 + Seconds * 1000 + MilliSeconds;
				num2 = Days2 * 86400000 + Hours2 * 3600000 + Minutes2 * 60000 + Seconds2 * 1000 + MilliSeconds2;
				Globals.Manager.Write("&nbsp;&nbsp;(" + PrintTimeDiff3(num2, num) + ")");
			}
			Globals.Manager.Write("</td></tr>");
			thread.GetKernelTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
			Globals.Manager.Write("<tr><td nowrap>Kernel time</td><td  nowrap>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintTime2(Days, Hours, Minutes, Seconds, MilliSeconds));
			if (lastThread != null)
			{
				lastThread.GetKernelTime(out Days2, out Hours2, out Minutes2, out Seconds2, out MilliSeconds2);
				num = Days * 86400000 + Hours * 3600000 + Minutes * 60000 + Seconds * 1000 + MilliSeconds;
				num2 = Days2 * 86400000 + Hours2 * 3600000 + Minutes2 * 60000 + Seconds2 * 1000 + Convert.ToInt32(MilliSeconds2);
				Globals.Manager.Write("&nbsp;&nbsp;(" + PrintTimeDiff3(num2, num) + ")");
			}
			Globals.Manager.Write("</td></tr>");
		}
		Globals.Manager.Write("</table><br><br>");
	}

	private static void ReportOperationDetails(COperation operation)
	{
		int num = 0;
		double num2 = 0.0;
		double num3 = 0.0;
		CacheFunctions.ScriptThreadClass thread = null;
		CacheFunctions.ScriptThreadClass lastThread = null;
		bool flag = operation.FunctionsByHitCount != null && operation.FunctionsByHitCount.Count > 0;
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection(operation.Key, (SectionType)0);
		val.Title = operation.Name;
		val.Collapsible = true;
		val.Collapsed = !flag;
		Globals.Manager.CurrentSection = val;
		val.Write("<tr><td><a name='Operation:" + Convert.ToString(operation.Key) + "'></a>");
		val.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
		if (operation.OpType != 1)
		{
			val.Write("<tr><td nowrap>Type</td><td nowrap>&nbsp;&nbsp;<b>" + operation.TypeString + "</b></td></tr>");
		}
		val.Write("<tr><td nowrap>Dumps present</td><td nowrap>&nbsp;&nbsp;" + operation.DumpsPresent + "</td></tr>");
		num2 = operation.DurationMin;
		num3 = operation.DurationMax;
		if (num2 == num3)
		{
			val.Write("<tr><td nowrap>Elapsed Time</td><td nowrap>&nbsp;&nbsp;<b>" + MaybeUnknownTime("", num2) + "</b></td></tr>");
		}
		else
		{
			val.Write("<tr><td nowrap>Elapsed Time<br>&nbsp;&nbsp;&nbsp;&nbsp;Minimum:<br>&nbsp;&nbsp;&nbsp;&nbsp;Maximum:</td><td nowrap>&nbsp;&nbsp;<br>&nbsp;&nbsp;" + Convert.ToString(MaybeUnknownTime("At least", operation.DurationMin)) + "<br>&nbsp;&nbsp;" + Convert.ToString(MaybeUnknownTime("At most", operation.DurationMax)) + "</td></tr>");
		}
		if (operation.OpType > 1)
		{
			val.Write("<tr><td><br><br></td></tr><table border=0 cellpadding=0 cellspacing=0 class=myCustomText><th><u>" + operation.TypeString + " Info</u></th>");
			operation.GetCustomInfoReport();
		}
		val.Write("</table><br><br>");
		if (!operation.SpansThreads)
		{
			using (Dictionary<int, CacheFunctions.ScriptThreadClass>.ValueCollection.Enumerator enumerator = operation.ThreadsByDumpNumber.Values.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					thread = enumerator.Current;
				}
			}
			ReportThreadInfo(lastThread, thread, showGeneral: true, showTimes: false);
		}
		if (flag)
		{
			operation.ShowStats("Function Stats", "");
			val = Globals.Manager.CurrentSection.AddChildSection(operation.Key + "-stacks", (SectionType)0);
			val.Title = "Call Stacks";
			val.Collapsible = true;
			Globals.Manager.CurrentSection = val;
			val.Write("<br><table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr>");
			for (num = 1; num <= Globals.g_dumps.Count; num++)
			{
				if (!operation.ThreadsByDumpNumber.ContainsKey(num))
				{
					val.Write("<td style='padding-right:20px;color:gray;text-align:center;vertical-align:middle'>");
					val.Write("<i>(operation not present in Dump " + num + ")</i>");
					continue;
				}
				val.Write("<td style='padding-right:20px;text-align:left;vertical-align:bottom'>");
				val.Write("<u>" + GetDumpLink(num) + "</u><br><br>");
				ReportCallStack(operation.RelevantStackFramessByTime[num], operation);
				thread = operation.ThreadsByDumpNumber[num];
				ReportThreadInfo(lastThread, thread, operation.SpansThreads, showTimes: true);
				lastThread = thread;
				val.Write("</td>");
			}
			val.Write("</tr></table>");
			val.Write("<br>");
			val.Write("<a href='#'>Back to Top</a><br><br></td></tr>");
			Globals.Manager.CurrentSection = val.Parent;
			val = val.Parent;
		}
		Globals.Manager.CurrentSection = val.Parent;
	}

	public static string FormatFunctionName(string fnName, bool bChop, IHasTopUserFunctions operationOrOperations)
	{
		string text = "";
		int num = 0;
		int num2 = 0;
		string text2 = null;
		string text3 = "";
		int num3 = 0;
		text2 = ((!bChop) ? fnName : ChopFunctionNameForGraph(fnName));
		num3 = fnName.LastIndexOf(")");
		text3 = ((num3 <= 0) ? fnName.ToUpper() : fnName.Substring(0, num3).ToUpper());
		if (Globals.g_AllOperations.AllBoilerPlateFunctions.Contains(text3))
		{
			return FormatFunction(text2, 1, -1, operationOrOperations);
		}
		if (Convert.ToBoolean(Globals.g_AllOperations.FunctionsByHitCount.ContainsKey(fnName)))
		{
			int[] array = Globals.g_AllOperations.FunctionsByHitCount[fnName];
			num = array[0];
			num2 = array[1];
			return FormatFunction(text2, num2, Convert.ToInt32(num), operationOrOperations);
		}
		if (text3.IndexOf("WAITFOR") >= 0)
		{
			return FormatFunction(text2, 1, 0, operationOrOperations);
		}
		return Convert.ToString(fnName);
	}

	public static string ChopFunctionNameForGraph(string FunctionName)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		StringBuilder stringBuilder = null;
		num3 = 100;
		num2 = 1;
		stringBuilder = new StringBuilder();
		num = FunctionName.GetSafeLength();
		if (num == 0)
		{
			return "";
		}
		for (; num2 + num3 < num; num2 += num3)
		{
			stringBuilder.Append(FunctionName.Substring(num2 - 1, num3));
			stringBuilder.Append("<BR>");
		}
		stringBuilder.Append(FunctionName.Substring(num2 - 1));
		return stringBuilder.ToString();
	}

	public static string FormatFunction(string fnPrototype, int fnPriority, int fnHitCount, IHasTopUserFunctions operationOrOperations)
	{
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		string text6 = "";
		string text7 = "";
		bool flag = false;
		bool flag2 = false;
		string text8 = "";
		bool flag3 = false;
		bool flag4 = false;
		int num = 0;
		num = fnPrototype.IndexOf("(");
		if (num >= 0)
		{
			text5 = Convert.ToString(fnPrototype).Substring(0, num);
			text6 = Convert.ToString(fnPrototype).Substring(num);
		}
		else
		{
			text5 = Convert.ToString(fnPrototype);
			text6 = "";
		}
		switch (fnPriority)
		{
		case 1:
			text7 = "#CCCCCC";
			text8 = "#CCCCCC";
			break;
		case 2:
			text7 = "#777777";
			text8 = "#777777";
			break;
		case 3:
			text7 = "blue";
			text8 = "black";
			if (operationOrOperations != null && operationOrOperations.IsTopUserFunction(fnPrototype))
			{
				flag2 = true;
				flag4 = true;
			}
			if (Globals.g_AllOperations.IsTopUserFunction(fnPrototype))
			{
				text7 = "red";
				flag = true;
				flag3 = false;
			}
			break;
		}
		if (text7 != "")
		{
			text = "<font color='" + text7 + "'>";
			text2 = "</font>";
		}
		if (flag)
		{
			text += "<b>";
			text2 = "</b>" + text2;
		}
		if (flag2)
		{
			text += "<i>";
			text2 = "</i>" + text2;
		}
		if (text8 != "")
		{
			text3 = "<font color='" + text8 + "'>";
			text4 = "</font>";
		}
		if (flag3)
		{
			text3 += "<b>";
			text4 = "</b>" + text4;
		}
		if (flag4)
		{
			text3 += "<i>";
			text4 = "</i>" + text4;
		}
		return text + text5 + text2 + text3 + text6 + text4;
	}

	private static string MaybeUnknownTime(string qualifier, double seconds)
	{
		string text = "";
		if (seconds == 0.0)
		{
			return "(unknown)";
		}
		if (qualifier != "")
		{
			qualifier += " ";
		}
		return qualifier + "<b>" + Globals.HelperFunctions.PrintTime(seconds) + "</b>";
	}

	private static string PrintTimeDiff(double time1, double time2)
	{
		string result = "";
		if (time2 > time1)
		{
			result = "+" + Globals.HelperFunctions.PrintTime(time2 - time1);
		}
		else if (time2 < time1)
		{
			result = "-" + Globals.HelperFunctions.PrintTime(time1 - time2);
		}
		return result;
	}

	private static string PrintTimeDiff3(double time1, double time2)
	{
		string result = "";
		if (time2 > time1)
		{
			result = "+" + Globals.HelperFunctions.PrintTime3(time2 - time1);
		}
		else if (time2 < time1)
		{
			result = "-" + Globals.HelperFunctions.PrintTime3(time1 - time2);
		}
		return result;
	}

	private string Truncate(string str, int size)
	{
		string text = "";
		if (str.GetSafeLength() > size)
		{
			return str.Substring(0, size);
		}
		return str;
	}

	private static string GetDumpLink(int dumpNumber)
	{
		return "<a href='#DumpInfo_" + Convert.ToString(dumpNumber) + "'>Dump " + Convert.ToString(dumpNumber) + "</a>";
	}

	private string NoSpaces(string str)
	{
		return Globals.HelperFunctions.Replace(str, " ", "");
	}

	public static void AnalyzeOperations()
	{
		Globals.g_AllOperations.Sort();
		Globals.g_AllOperations.RollupFunctionsByHitCount();
	}

	public static void LoadOperationsForDump(CDump dump, object dumpNum, object totalDumps)
	{
		string text = "";
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if (Convert.ToInt32(dumpNum) > 0)
		{
			text = " - Dump #" + Convert.ToString(dumpNum) + " of " + Convert.ToString(totalDumps);
		}
		Globals.HelperFunctions.ResetStatus("Scanning for operations" + text, Globals.g_ThreadInfoCache.Count, "Thread");
		if (Globals.g_MinThreadOverride <= 0)
		{
			num2 = 0;
			num3 = Globals.g_ThreadInfoCache.Count - 1;
		}
		else
		{
			num2 = Globals.g_MinThreadOverride;
			num3 = Globals.g_MaxThreadOverride;
		}
		for (num = num2; num <= num3; num++)
		{
			Globals.HelperFunctions.IncrementSubStatus();
			LoadOperationsForThread(Globals.g_ThreadInfoCache.Item(num), dump);
		}
		Globals.HelperFunctions.ClearSubStatus();
	}

	private static void LoadOperationsForThread(CacheFunctions.ScriptThreadClass thread, CDump dump)
	{
		int num = 0;
		num = Globals.g_AllOperations.Count;
		if ((dump.IsMiniDump || (AddASPOperations(thread, dump) && AddASPNETOperations(thread, dump))) && AddCOMOperations(thread) && num == Globals.g_AllOperations.Count)
		{
			AddUnknownOperation(thread, dump);
		}
	}

	private static bool AddCOMOperations(CacheFunctions.ScriptThreadClass thread)
	{
		return true;
	}

	private static bool AddASPNETOperations(CacheFunctions.ScriptThreadClass thread, CDump dump)
	{
		COperation cOperation = null;
		string text = "";
		int num = 0;
		cOperation = new COperation();
		num = dump.DumpNumber;
		text = Convert.ToString(thread.SystemID) + "_" + Convert.ToString(AnalyzeThreads.GetTEBAsHex(thread));
		if (Convert.ToString(thread.ClrStackReportNoArgsNoColor).ToUpper().IndexOf("SYSTEM.WEB.HTTPAPPLICATION.EXECUTESTEP") >= 0)
		{
			cOperation.BeginInit(3, text, thread, null, num);
			Globals.g_AllOperations.AddOperation(cOperation, num);
			dump.OperationsInDump.AddOperation(cOperation, num);
			return false;
		}
		return true;
	}

	private static bool AddASPOperations(CacheFunctions.ScriptThreadClass thread, CDump dump)
	{
		if (dump.ASPInfo == null)
		{
			return true;
		}
		bool flag = false;
		string text = "";
		int num = 0;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		string text2 = null;
		int num2 = 0;
		IASPRequest iASPRequest = null;
		COperation cOperation = null;
		int num3 = 0;
		flag = true;
		iASPRequest = dump.ASPInfo.GetASPRequestByThreadID(Convert.ToInt32(thread.ThreadID));
		if (iASPRequest != null)
		{
			flag = false;
			num = thread.FindFrameInStack("ASP!EXECUTE+");
			if (num > -1)
			{
				scriptStackFrameClass = thread.StackFrames[num];
				if (scriptStackFrameClass != null)
				{
					text = Convert.ToInt32(scriptStackFrameClass.Args(1)).ToString("X");
				}
			}
			if (text.Equals(""))
			{
				text2 = dump.Debugger.Execute("~" + Convert.ToString(thread.ThreadID) + "e ddp esp poi(@$teb+4)");
				num2 = text2.IndexOf("asp!CHitObj::`vftable'");
				if (num2 >= 0)
				{
					text = Convert.ToString(text2).Substring(num2 - 18, 8);
				}
			}
			if (!text.Equals(""))
			{
				cOperation = new COperation();
				num3 = dump.DumpNumber;
				cOperation.BeginInit(2, Convert.ToString(thread.SystemID) + "_" + text, thread, iASPRequest, num3);
				Globals.g_AllOperations.AddOperation(cOperation, num3);
				dump.OperationsInDump.AddOperation(cOperation, num3);
			}
		}
		return flag;
	}

	private static void AddUnknownOperation(CacheFunctions.ScriptThreadClass thread, CDump dump)
	{
		COperation cOperation = null;
		string text = "";
		int num = 0;
		cOperation = new COperation();
		num = dump.DumpNumber;
		text = Convert.ToString(thread.SystemID) + "_" + Convert.ToString(AnalyzeThreads.GetTEBAsHex(thread));
		cOperation.BeginInit(1, text, thread, null, num);
		Globals.g_AllOperations.AddOperation(cOperation, num);
		dump.OperationsInDump.AddOperation(cOperation, num);
	}
}
