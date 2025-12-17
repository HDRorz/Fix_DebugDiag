using System.Collections.Generic;
using DebugDiag.DotNet.Reports;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

internal class ReportASPInfoImpl : IReportASPInfo
{
	public void Report_ASPInfo()
	{
		IASPInfo g_ASPInfo = Globals.g_ASPInfo;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		int num = g_ASPInfo.CurrentRequests.Count + g_ASPInfo.ASPApps.Count + g_ASPInfo.TemplateCache.Count;
		CacheFunctions.ScriptModuleClass moduleByName = Globals.g_ModuleCache.GetModuleByName("asp");
		string text;
		if (moduleByName == null)
		{
			text = "Unable to obtain Module information for asp.dll";
		}
		else
		{
			moduleByName.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
			text = Major + "." + Minor + "." + Build + "." + Priv;
		}
		if (num == 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Analyzing and reporting ASP information", num, "ASP Item");
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("ASPReport", (SectionType)0);
		val.Title = "ASP report";
		val.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText ID='Table2'>");
		val.Write("<tr><td>Executing ASP requests</td><td>&nbsp;&nbsp;<b>" + g_ASPInfo.CurrentRequests.Count + "</b> Request(s)</td></tr>");
		val.Write("<tr><td>ASP templates cached</td><td>&nbsp;&nbsp;<b>" + g_ASPInfo.TemplateCache.Count + "</b> Template(s)</td></tr>");
		val.Write("<tr><td>ASP template cache size</td><td>&nbsp;&nbsp;<b>" + GetTemplateCacheSize(g_ASPInfo) + "</b></td></tr>");
		val.Write("<tr><td>Loaded ASP applications</td><td>&nbsp;&nbsp;<b>" + g_ASPInfo.ASPApps.Count + "</b> Application(s)</td></tr>");
		val.Write("<tr><td>ASP.DLL Version </td><td>&nbsp;&nbsp;<b>" + text + "</b></td></tr>");
		val.Write("</table><br>");
		ReportSection currentSection = Globals.Manager.CurrentSection;
		if (g_ASPInfo.ASPApps.Count > 0)
		{
			val = Globals.Manager.CurrentSection.AddChildSection("ASPapplicationreport", (SectionType)0);
			val.Title = "ASP application report";
			Globals.Manager.CurrentSection = val;
		}
		foreach (IASPApplication aSPApp in g_ASPInfo.ASPApps)
		{
			ReportASPApp(aSPApp);
			Globals.HelperFunctions.IncrementSubStatus();
		}
		Globals.Manager.CurrentSection = currentSection;
		if (g_ASPInfo.CurrentRequests.Count > 0)
		{
			val = Globals.Manager.CurrentSection.AddChildSection("ASPRequestReport", (SectionType)0);
			val.Title = "Current ASP Request report";
			Globals.Manager.CurrentSection = val;
		}
		foreach (IASPRequest currentRequest in g_ASPInfo.CurrentRequests)
		{
			if (currentRequest.SecondsAlive >= Globals.g_RequestTimeLimit)
			{
				Globals.g_LongRunningASPReq++;
			}
			ReportASPRequest(currentRequest);
			Globals.HelperFunctions.IncrementSubStatus();
		}
		Globals.Manager.CurrentSection = currentSection;
		if (g_ASPInfo.TemplateCache.Count > 0)
		{
			val = Globals.Manager.CurrentSection.AddChildSection("ASPTemplateReport", (SectionType)0);
			val.Title = "ASP Template Cache report";
			Globals.Manager.CurrentSection = val;
		}
		foreach (IASPTemplate item in g_ASPInfo.TemplateCache)
		{
			ReportASPTemplate(item);
			Globals.HelperFunctions.IncrementSubStatus();
		}
		Globals.Manager.CurrentSection = currentSection;
		int num2 = FileWithMorethan10Includes();
		if (num2 > 0)
		{
			Globals.Manager.ReportWarning("<b>" + num2 + " ASP Template file(s) </b> are including more than 10 include files. The size of the ASP template cache increases significantly when additional ASP files and a potentially large number of include files are  added to the cache. You may also notice slow performance and increased memory usage on the Web server.", "Check the <a  href='#" + Globals.g_UniqueReference + "ASPTemplateReport'>ASP Template Cache Report </a> to view the number of  ASP templates that are cached, the size of each ASP template, and the ASP template include file hierarchy. Also check if there is any heap fragmentation reported in the <u>ASP Template heap</u> by running the <b>Memory Pressure Analyzers</b> script in Debug Diagnostic Tool.  For more details on this issue refer  to <b><a href='http://support.microsoft.com/kb/914156'>http://support.microsoft.com/kb/914156</a></b>", 0, "{5d79c985-3cf5-49c8-b0d6-7a9d20147eb0}");
		}
	}

	public int FileWithMorethan10Includes()
	{
		IASPInfo g_ASPInfo = Globals.g_ASPInfo;
		if (g_ASPInfo.TemplateCache.Count > 0)
		{
			int num = 0;
			foreach (IASPTemplate item in g_ASPInfo.TemplateCache)
			{
				if (item.Count > 10)
				{
					num++;
				}
			}
			if (num > 0)
			{
				return num;
			}
		}
		return 0;
	}

	public void OutputVBScriptStack(IASPRequest ASPRequest)
	{
		string text = "";
		Globals.Manager.Write("Script call stack for thread <b>" + ASPRequest.ThreadID + "</b><br><br>");
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
		Globals.Manager.Write("<tr><th>Function Scope</th><th>&nbsp;&nbsp;Line Of Code</th><th>&nbsp;&nbsp;Source File</th><th>&nbsp;&nbsp;Line Number</th></tr>");
		foreach (IScriptFrame item in ASPRequest)
		{
			if (item.FunctionName == "")
			{
				Globals.Manager.Write("<tr><td nowrap>Global Scope</td>");
			}
			else
			{
				Globals.Manager.Write("<tr><td nowrap>" + item.FunctionName + "</td>");
			}
			text = item.LineOfCode.Replace("<", "&lt");
			text = text.Replace(">", "&gt");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + text + "</td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + item.SourceFile + "</td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + item.LineNumber + "</td>");
			Globals.Manager.Write("</tr>");
		}
		Globals.Manager.Write("</table>");
		Globals.Manager.Write("<br><br>");
	}

	public void ReportASPRequest(IASPRequest ASPRequest)
	{
		Globals.Manager.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText ID=\"Table1\">");
		Globals.Manager.Write("<tr><td>ASP request executing on thread</td><td>&nbsp;&nbsp;" + Globals.HelperFunctions.GetThreadIDWithLink(ASPRequest.ThreadID) + "</td></tr>");
		Globals.Manager.Write("<tr><td><b>" + ASPRequest.Method + "</b> request for</td><td>&nbsp;&nbsp;<b>" + ASPRequest.VirtualPath + "</b></td><tr>");
		Globals.Manager.Write("<tr><td>Request alive for</td><td>&nbsp;&nbsp;<b>" + Globals.HelperFunctions.PrintTime(ASPRequest.SecondsAlive) + "</b></td><tr>");
		Globals.Manager.Write("<tr><td>QueryString</td><td>&nbsp;&nbsp;" + ASPRequest.QueryString + "</td><tr>");
		Globals.Manager.Write("<tr><td>Request mapped to</td><td>&nbsp;&nbsp;" + ASPRequest.PhysicalPath + "</td><tr>");
		if (ASPRequest.Application != null)
		{
			Globals.Manager.Write("<tr><td>ASP Application</td><td>&nbsp;&nbsp;" + Globals.HelperFunctions.GetASPAppWithLink(ASPRequest.Application) + "</td><tr>");
		}
		if (ASPRequest.Template != null)
		{
			Globals.Manager.Write("<tr><td>ASP Template</td><td>&nbsp;&nbsp;" + Globals.HelperFunctions.GetASPTemplateWithLink(ASPRequest.Template) + "</td><tr>");
		}
		Globals.Manager.Write("</table>");
		Globals.Manager.Write("<br><br>");
	}

	public void PrintASPFile(IASPFile CurrentFile, int Nesting)
	{
		string text = "";
		for (int i = 1; i <= Nesting; i++)
		{
			text += "&nbsp;&nbsp;&nbsp;";
		}
		Globals.Manager.Write(string.Concat(text, CurrentFile.PhysicalPath, "&nbsp;&nbsp;&nbsp;", CurrentFile.LastModifiedTime, "<br>"));
		if (CurrentFile.ChildFile != null)
		{
			PrintASPFile(CurrentFile.ChildFile, Nesting++);
		}
		for (IASPFile siblingFile = CurrentFile.SiblingFile; siblingFile != null; siblingFile = siblingFile.SiblingFile)
		{
			Globals.Manager.Write(string.Concat(text, siblingFile.PhysicalPath, "&nbsp;&nbsp;&nbsp;", siblingFile.LastModifiedTime, "<br>"));
			if (siblingFile.ChildFile != null)
			{
				PrintASPFile(siblingFile.ChildFile, Nesting++);
			}
		}
	}

	public void ReportASPTemplate(IASPTemplate ASPTemplate)
	{
		Globals.Manager.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText>");
		Globals.Manager.Write("<tr><td>ASP Template for</td><td>&nbsp;&nbsp;<a name='" + Globals.g_UniqueReference + "ASPTemplate" + ASPTemplate.ApplicationURL + ASPTemplate.PhysicalPath + "'><b>" + ASPTemplate.PhysicalPath + "</b></a></td></tr>");
		Globals.Manager.Write("<tr><td>Application URL</td><td>&nbsp;&nbsp;<b>" + ASPTemplate.ApplicationURL + "</b></td></tr>");
		Globals.Manager.Write("<tr><td>Application Path</td><td>&nbsp;&nbsp;" + ASPTemplate.ApplicationPath + "</td></tr>");
		Globals.Manager.Write("<tr><td>Template Size</td><td>&nbsp;&nbsp;" + ASPTemplate.TemplateSize + " bytes</td></tr>");
		Globals.Manager.Write("<tr><td># of files in Template</td><td>&nbsp;&nbsp;" + ASPTemplate.Count + " file(s)</td></tr>");
		Globals.Manager.Write("<tr><td colspan=2>Include Heirarchy</td></tr>");
		Globals.Manager.Write("<tr><td colspan=2 nowrap>");
		PrintASPFile(ASPTemplate.RootFile, 2);
		Globals.g_Debugger.Write("%></td></tr>");
		Globals.Manager.Write("</table><br>");
	}

	public void ReportASPApp(IASPApplication ASPApp)
	{
		Globals.Manager.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText><tr><td>ASP application metabase key</td><td>&nbsp;&nbsp;<a name='" + Globals.g_UniqueReference + "ASPApp" + ASPApp.MetabaseKey + "'><b><%=ASPApp.MetabaseKey%></b></a></td></tr><tr><td>Physical Path</td><td>&nbsp;&nbsp;<b><%=ASPApp.PhysicalPath%></b></td></tr><tr><td>Virtual Root</td><td>&nbsp;&nbsp;<b><%=ASPApp.VirtualPath%></b></td></tr><tr><td>Session Count</td><td>&nbsp;&nbsp;<b><%=ASPApp.SessionCount%> Session(s)</b></td></tr><tr><td>Request Count</td><td>&nbsp;&nbsp;<b><%=ASPApp.RequestCount%> Request(s)</b></td></tr><tr><td>Session Timeout</td><td>&nbsp;&nbsp;<%=ASPApp.SessionTimeout%> minutes(s)</td></tr><tr><td>Path to Global.asa</td><td>&nbsp;&nbsp;<%=ASPApp.GlobalASAPath%></td></tr><tr><td>Server side script debugging enabled</td><td>&nbsp;&nbsp;<%=ASPApp.AllowDebugging%></td></tr><tr><td>Client side script debugging enabled</td><td>&nbsp;&nbsp;<%=ASPApp.AllowClientDebugging%></td></tr><tr><td>Out of process COM servers allowed</td><td>&nbsp;&nbsp;<%=ASPApp.AllowOutOfProcComponents%></td></tr><tr><td>Session state turned on</td><td>&nbsp;&nbsp;<%=ASPApp.AllowSessionState%></td></tr><tr><td>Write buffering turned on</td><td>&nbsp;&nbsp;<%=ASPApp.BufferingOn%></td></tr><tr><td>Application restart enabled</td><td>&nbsp;&nbsp;<%=ASPApp.EnableAppRestart%></td></tr><tr><td>Parent paths enabled</td><td>&nbsp;&nbsp;<%=ASPApp.EnableParentPaths%></td></tr><tr><td>ASP Script error messages will be sent to browser</td><td>&nbsp;&nbsp;<%=ASPApp.ScriptErrorsSentToBrowser%></td></tr></table><br>");
	}

	public string GetTemplateCacheSize(IASPInfo ASPInfo)
	{
		double num = 0.0;
		foreach (IASPTemplate item in ASPInfo.TemplateCache)
		{
			num += item.TemplateSize;
		}
		if (num > 10485760.0)
		{
			return Globals.HelperFunctions.FormatNumber(num / 1048576.0, 2) + " MBytes";
		}
		if (num > 10240.0)
		{
			return Globals.HelperFunctions.FormatNumber(num / 1024.0, 2) + " KBytes";
		}
		return Globals.HelperFunctions.FormatNumber(num, 2) + " Bytes";
	}

	public bool IsAspAppRestarting(CacheFunctions.ScriptThreadClass Thread)
	{
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = Thread.StackFrames;
		for (int i = 0; i < stackFrames.Count - 1; i++)
		{
			if (CacheFunctions.GetFunctionName(stackFrames[i].InstructionAddress).Contains("ASP!CAPPLN::RESTART"))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsASPDebuggingEnabled()
	{
		Dictionary<string, IASPApplication> dictionary = new Dictionary<string, IASPApplication>();
		IASPInfo g_ASPInfo = Globals.g_ASPInfo;
		if (g_ASPInfo.ASPApps.Count > 0)
		{
			foreach (IASPApplication aSPApp in g_ASPInfo.ASPApps)
			{
				if (aSPApp.AllowDebugging && !dictionary.ContainsKey(aSPApp.MetabaseKey))
				{
					dictionary.Add(aSPApp.MetabaseKey, aSPApp);
				}
			}
			if (dictionary.Count > 0)
			{
				string text = "The following ASP application(s) have ASP server side script debugging enabled: <br><br>";
				foreach (string key in dictionary.Keys)
				{
					text = text + "<b><a href=#" + Globals.g_UniqueReference + "ASPApp" + key + ">" + key + "</a></b><br>";
				}
				Globals.Manager.ReportError(text, "Enabling server side script debugging can result in performance issues with an ASP application, and result in a hang condition when multiple users are browsing the web site. Please see the following knowledge base article for more information:<br><br><b><a target='_blank' href=http://support.microsoft.com/?id=312941>PRB: IIS May Hang If You Enable ASP Server-Side Script Debugging</a></b>", 1000, "{9ad48b9c-1545-4296-a273-40af81e5f96e}");
				return true;
			}
			return false;
		}
		return false;
	}
}
