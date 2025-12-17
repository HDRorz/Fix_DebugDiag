using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugDiag.DbgLib;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public class AnalyzedThreadClass
{
	public CacheFunctions.ScriptThreadClass Thread;

	public string Description = "";

	public string Recommendation = "";

	public string Category = "";

	public string AdditionalInfo = "";

	public string AdditionalCLRInfo = "";

	public string KeyPartOne = "";

	public string KeyPartTwo = "";

	public bool IsWarning;

	public bool IsError;

	public Dictionary<int, CacheFunctions.ScriptThreadClass> BlockedThreads = new Dictionary<int, CacheFunctions.ScriptThreadClass>();

	public int BlockedASPCount;

	public int BlockedClientConnCount;

	public int Weight;

	public IClientConnection ClientConnInfo;

	public IASPRequest ASPRequestInfo;

	public bool IsIssue
	{
		get
		{
			switch (Category)
			{
			case "OK":
			case "COMCALLSTA":
			case "COMCALL":
			case "BADTEB":
			case "UNRESOLVED":
				return false;
			default:
				return true;
			}
		}
	}

	private bool HasWaiters
	{
		get
		{
			if (IsIssue)
			{
				if (Globals.g_ThreadsWithSyncBlkWaiters.Contains(Thread.ThreadID.ToString()))
				{
					return true;
				}
				if ((!string.IsNullOrEmpty(Thread.RpcSourceBindings) && !Thread.RpcSourceBindings.ToLower().StartsWith("error")) || (!string.IsNullOrEmpty(Thread.SocketSourceAddress) && !Thread.SocketSourceAddress.ToLower().StartsWith("error")))
				{
					return true;
				}
				if (Globals.g_collCritSecs.Where((KeyValuePair<double, IDbgCritSec> kvp) => kvp.Value.OwnerThreadID == Thread.ThreadID && kvp.Value.LockCount > 0).Any())
				{
					return true;
				}
			}
			return false;
		}
	}

	public string getBlockingReport()
	{
		StringBuilder stringBuilder = null;
		object obj = null;
		stringBuilder = new StringBuilder();
		if (Description.ToLower().EndsWith("</div>"))
		{
			Description += "<br>";
		}
		else
		{
			Description += "<br><br>";
		}
		stringBuilder.Append("The following threads in <b>" + Globals.g_ShortDumpFileName + "</b> are " + Description + "(");
		foreach (int key in BlockedThreads.Keys)
		{
			stringBuilder.Append(" " + Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(key)));
		}
		stringBuilder.Append(" )<br><br>");
		if (BlockedASPCount > 0)
		{
			obj = FormatPercentThreads(BlockedASPCount, Globals.g_ASPInfo.CurrentRequests.Count);
			stringBuilder.Append("<b>" + obj.ToString() + "%</b> of executing ASP Requests blocked<br><br>");
		}
		if (BlockedClientConnCount > 0)
		{
			obj = FormatPercentThreads(BlockedClientConnCount, Globals.g_HTTPInfo.ATQThreadCount);
			stringBuilder.Append("<b>" + obj.ToString() + "%</b> of IIS worker threads blocked<br><br>");
		}
		obj = FormatPercentThreads(BlockedThreads.Count, Globals.g_ThreadInfoCache.Count);
		stringBuilder.Append("<b>" + obj.ToString() + "%</b> of threads blocked (" + Convert.ToString(BlockedThreads.Count) + " threads)<br><br>");
		return stringBuilder.ToString();
	}

	private object FormatPercentThreads(double BlockedCount, double TotalCount)
	{
		object obj = null;
		obj = BlockedCount * 100.0 / TotalCount;
		return $"{obj:N2}";
	}

	public void AddCount(int amount, int Counter)
	{
		switch (Counter)
		{
		case 1:
			Weight += amount;
			break;
		case 2:
			BlockedASPCount += amount;
			break;
		case 3:
			BlockedClientConnCount += amount;
			break;
		}
	}

	internal void AdjustCategoryIfHasWaiters()
	{
		if (HasWaiters)
		{
			Category += "_HASWAITERS";
		}
	}
}
