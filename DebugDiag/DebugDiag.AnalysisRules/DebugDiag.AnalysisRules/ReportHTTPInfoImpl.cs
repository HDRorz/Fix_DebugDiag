using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugDiag.DotNet.Reports;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

internal class ReportHTTPInfoImpl : IReportHTTPInfo
{
	private int g_NumWaitingConnections;

	private Dictionary<string, int> g_AspNetSessionIDsDictionary;

	public void Report_HTTPInfo()
	{
		IHTTPInfo g_HTTPInfo = Globals.g_HTTPInfo;
		if (g_HTTPInfo.Count == 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Analyzing and reporting HTTP information", g_HTTPInfo.Count, "HTTP Info");
		g_NumWaitingConnections = 0;
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("HTTPreport", (SectionType)0);
		val.Title = "HTTP report";
		val.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText ID='Table1'><tr><td>IIS ThreadPool worker thread count</td><td>&nbsp;&nbsp;<b>" + g_HTTPInfo.ATQThreadCount + "</b> Thread(s)</td></tr><tr><td>Available ThreadPool worker thread count</td><td>&nbsp;&nbsp;<b>" + g_HTTPInfo.AvailableATQThreads + "</b> Thread(s)</td></tr><tr><td>Active client connections</td><td>&nbsp;&nbsp;<b>" + g_HTTPInfo.Count + "</b> client connection(s)</td></tr></table><br>");
		ReportSection currentSection = Globals.Manager.CurrentSection;
		val = Globals.Manager.CurrentSection.AddChildSection("ClientConnections", (SectionType)0);
		val.Title = "Client Connections";
		g_AspNetSessionIDsDictionary = null;
		g_AspNetSessionIDsDictionary = new Dictionary<string, int>();
		Globals.Manager.CurrentSection = val;
		int num = 0;
		if (!int.TryParse(ConfigHelper.GetSetting("maxIISConnections"), out var result))
		{
			result = 100;
		}
		foreach (object item in (dynamic)g_HTTPInfo.SortClientConnectionsBySecondsAlive())
		{
			IClientConnection clientConn = (IClientConnection)(dynamic)item;
			if (num++ < result)
			{
				ReportClientConnection(clientConn);
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		if (num > result)
		{
			Globals.Manager.WriteLine($"<br>Details were only displayed for the first {result} out of {num} IIS connections.  To view more details, modify the <b>maxIISConnections</b> setting in the DebugDiag.AnalysisRules.dll.config file and rerun the analysis");
		}
		Globals.Manager.CurrentSection = currentSection;
		string text = "";
		foreach (string item2 in g_AspNetSessionIDsDictionary.Keys.OrderBy((string x) => x))
		{
			if (g_AspNetSessionIDsDictionary[item2] > 1)
			{
				text = text + "<tr><td>" + item2 + "</td><td>" + g_AspNetSessionIDsDictionary[item2] + "</td><tr>";
			}
		}
		if (g_NumWaitingConnections > 0)
		{
			val.Write("<p><b>" + g_NumWaitingConnections + "</b> connection(s) waiting for the next request.</p>");
		}
		if (text != "")
		{
			text = "<table><th>Session Id</th><th>Requests</th>" + text + "</table>";
			Globals.Manager.ReportError("Multiple requests in the process state with the same ASP.NET Session ID were detected in the dump file. At any point of time, ASP.NET executes only one request with the same session id and the remaining requests are queued behind the request which is getting executed", "Please check why you got more than one request for the same ASP.NET Session ID by viewing the <a href='#HTTPReport" + Globals.g_UniqueReference + "'>HTTP report</a> and the <a href='#ASPNETSESSIONIDReport" + Globals.g_UniqueReference + "'>ASP.NET Session ID Report</a>", 0, "{3d32f378-8b72-4549-803c-709d8db5b784}");
			val = Globals.Manager.CurrentSection.AddChildSection("ASPNETSESSIONIDReport", (SectionType)0);
			val.Title = "ASP.NET Session ID report";
			val.Write("<br>" + text);
		}
	}

	private string test(string a, string b)
	{
		return b;
	}

	public bool IsClientConnReady(IClientConnection ClientConn)
	{
		return ((Globals.g_OSVER < Globals.OS_VER_WIN2K3) ? ClientConn.HTTPRequestState : ClientConn.NativeRequestState) switch
		{
			"NREQ_STATE_START" => false, 
			"NREQ_STATE_READ" => false, 
			"HTR_READING_CLIENT_REQUEST" => false, 
			_ => true, 
		};
	}

	public void ReportClientConnection(IClientConnection ClientConn)
	{
		if (IsClientConnReady(ClientConn))
		{
			if (ClientConn.SecondsAlive >= Globals.g_RequestTimeLimit)
			{
				Globals.g_LongRunningClientConns++;
			}
			Globals.Manager.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText><tr><td colspan=2 nowrap>Client connection from <b>" + ClientConn.RemoteIPAddress + ":" + ClientConn.RemotePort + "</b> to <b>" + ClientConn.LocalIPAddress + ":" + ClientConn.LocalPort + "</b></td></tr><tr><td>Host Header</td><td>&nbsp;&nbsp;<b>" + ClientConn.HostHeader + "</b></td></tr>");
			if (ClientConn.URL == ClientConn.OriginalURL || ClientConn.OriginalURL.GetSafeLength() == 0)
			{
				Globals.Manager.Write("<tr><td  nowrap><b>" + ClientConn.Verb + "</b> request for</td><td>&nbsp;&nbsp;<b>" + ClientConn.URL + "</b></td></tr>");
			}
			else
			{
				Globals.Manager.Write("<tr><td valign=top  nowrap><b>" + ClientConn.Verb + "</b> request for</td><td>&nbsp;&nbsp;<b>" + ClientConn.OriginalURL + "</b></td></tr><tr><td  nowrap>Mapped To URL</td><td>&nbsp;&nbsp;" + ClientConn.URL + "</td></tr>");
			}
			Globals.Manager.Write("<tr><td  nowrap>HTTP Version</td><td>&nbsp;&nbsp;" + ClientConn.HTTPVersion + "</td></tr><tr><td  nowrap>SSL Request</td><td>&nbsp;&nbsp;" + ClientConn.IsSecure + "</td></tr><tr><td  nowrap>Time alive</td><td>&nbsp;&nbsp;<b>" + Globals.HelperFunctions.PrintTime(ClientConn.SecondsAlive) + "</b></td></tr><tr><td  nowrap>QueryString</td><td>&nbsp;&nbsp;" + ClientConn.QueryString + "</td></tr><tr><td  nowrap>Request mapped to</td><td>&nbsp;&nbsp;" + ClientConn.PhysicalPath + "</td></tr><tr><td  nowrap>HTTP Request State</td><td>&nbsp;&nbsp;" + ClientConn.HTTPRequestState + "</td></tr>");
			if (Globals.g_OSVER >= Globals.OS_VER_WIN2K3)
			{
				Globals.Manager.Write("<tr><td  nowrap>Native Request State</td><td>&nbsp;&nbsp;" + ClientConn.NativeRequestState + "</td></tr>");
			}
			string aspNetSessionIdFromHTTPHeaders = GetAspNetSessionIdFromHTTPHeaders(ClientConn.HTTPHeaders);
			if (aspNetSessionIdFromHTTPHeaders != null)
			{
				Globals.Manager.Write("<tr><td  nowrap> ASP.NET Session ID</td><td>" + aspNetSessionIdFromHTTPHeaders + "</td></tr>");
			}
			if (Globals.Manager.IncludeHttpHeadersInClientConns)
			{
				Globals.Manager.Write("<tr><td  nowrap>HTTP Headers</td><td>&nbsp;&nbsp;" + (ClientConn.HTTPHeaders ?? "").Replace("\n", "<br/>") + "</td></tr>");
			}
			else
			{
				Globals.Manager.Write("<tr><td  nowrap>Client Connection State</td><td>&nbsp;&nbsp;" + ClientConn.ClientConnectionState + "</td></tr>");
			}
			if (ClientConn.ThreadID != -1)
			{
				Globals.Manager.Write("<tr><td  nowrap>Connection serviced by</td><td>&nbsp;&nbsp;Thread " + Globals.HelperFunctions.GetThreadIDWithLink(ClientConn.ThreadID) + "</td></tr>");
			}
			if (ClientConn.WAMProcessID != 0)
			{
				Globals.Manager.Write("<tr><td  nowrap>Out of Process ID (WAM)</td><td>&nbsp;&nbsp;Process " + ClientConn.WAMProcessID + "</td></tr>");
			}
			Globals.Manager.Write("</table><br><br>");
		}
		else
		{
			g_NumWaitingConnections++;
		}
	}

	public void ReportLongRunningRequests()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing pending web requests");
		if (Globals.g_LongRunningClientConns > 0)
		{
			IHTTPInfo g_HTTPInfo = Globals.g_HTTPInfo;
			foreach (IClientConnection item in g_HTTPInfo)
			{
				if (IsClientConnReady(item) && item.SecondsAlive >= Globals.g_RequestTimeLimit && item.WAMProcessID != Globals.g_Debugger.ProcessID && item.WAMProcessID != 0)
				{
					num++;
					if (!dictionary.ContainsKey(item.WAMProcessID))
					{
						dictionary.Add(item.WAMProcessID, item.WAMProcessID);
					}
				}
			}
			stringBuilder.Append("<b>" + Globals.g_LongRunningClientConns + "</b> client connection(s) in <b>" + Globals.g_ShortDumpFileName + "</b> have been executing a request for more than <b>" + Globals.g_RequestTimeLimit + " seconds</b>.");
			if (dictionary.Count > 0)
			{
				stringBuilder.Append("<br><br> Of those requests, <b>" + num + "</b> were executing in the following external process(es): <br><br>");
				foreach (int key in dictionary.Keys)
				{
					int num2 = 0;
					foreach (IClientConnection item2 in g_HTTPInfo)
					{
						if (item2.WAMProcessID == Convert.ToInt32(key) && item2.SecondsAlive >= Globals.g_RequestTimeLimit)
						{
							num2++;
						}
					}
					stringBuilder.Append("<font color=red><b>" + num2 + " connection(s) executing in PID " + key + "</b></font><br><br>");
				}
				stringBuilder.Append("Please run analysis on the associated dump to determine why these requests were still executing.");
			}
			Globals.Manager.ReportWarning(stringBuilder.ToString(), "Please see the <a href='#ClientConnections" + Globals.g_UniqueReference + "'>Client Connections</a> section of this report for more detailed information about the connection(s).", 0, "{9d52e076-1977-4789-9121-d694e768ad18}");
		}
		else if (Globals.g_LongRunningASPReq > 0)
		{
			stringBuilder.Append("<b>" + Globals.g_LongRunningASPReq + "</b> ASP Requests in <b>" + Globals.g_ShortDumpFileName + "</b> have been executing for more than <b>" + Globals.g_RequestTimeLimit + " seconds</b>.");
			Globals.Manager.ReportWarning(stringBuilder.ToString(), "Please see the <a href='#ASPRequestReport" + Globals.g_UniqueReference + "'>Current ASP Requests</a> section for more detailed information about the request(s) in question.", 0, "{4db2dff7-add0-409f-ba90-556f84ed3b0f}");
		}
	}

	private string GetAspNetSessionIdFromHTTPHeaders(string httpHeaders)
	{
		if (string.IsNullOrEmpty(httpHeaders))
		{
			return null;
		}
		string[] array = httpHeaders.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i <= Globals.HelperFunctions.UBound(array); i++)
		{
			if (array[i].IndexOf("Cookie") < 0)
			{
				continue;
			}
			string[] array2 = array[i].Split(new char[1] { ';' }, StringSplitOptions.None);
			for (int j = 0; j <= Globals.HelperFunctions.UBound(array2); j++)
			{
				if (array2[j].IndexOf("ASP.NET_SessionId") >= 0)
				{
					string text = array2[j];
					text = text.Replace("Cookie", "");
					text = text.Replace("ASP.NET_SessionId=", "");
					if (!g_AspNetSessionIDsDictionary.Keys.Contains(text))
					{
						g_AspNetSessionIDsDictionary.Add(text, 1);
					}
					else
					{
						g_AspNetSessionIDsDictionary[text] += 1;
					}
					return text;
				}
			}
		}
		return null;
	}
}
