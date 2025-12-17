using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DebugDiag.AnalysisRules.inc;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.Reports;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.RuntimeExt;
using Microsoft.VisualBasic;

namespace DebugDiag.AnalysisRules;

internal class AnalyzeManagedImpl : IAnalyzeManaged
{
	private int totalIOThreads;

	public void InitClrGlobals(bool doHeapScans = true)
	{
		Globals.g_clrModule = null;
		Globals.g_MaxConnectionErrorThreads = new int[0];
		Globals.g_MaxConnectionWarningThreads = new int[0];
		Globals.g_CLRExtensionExecuting = false;
		Globals.g_GCThread = -1;
		Globals.g_PreemptiveGCDisabledThreads = "";
		Globals.g_FinalizerThreadId = -1;
		Globals.g_FinalizerThreadBlocked = false;
		Globals.g_AttemptedToLoadSOS = false;
		Globals.g_IsWCFServiceHost = false;
		Globals.g_ManagedExceptionsPresent = false;
		Globals.g_threadCountManagedRunningMoreThan60Secs = 0;
		Globals.g_HttpContextsPresent = false;
		Globals.g_HttpContextMT = 0L;
		Globals.g_IsSystemWebApp = IsWebApp();
		if (Globals.g_IsSystemWebApp)
		{
			Globals.g_IsIIS7 = IsIIS7App();
		}
		Globals.g_HttpRequestQueueTable = null;
		if (doHeapScans)
		{
			Globals.g_IsWCFServiceHost = IsWcfServiceHost();
			Globals.g_IsWCFClient = IsWcfClient();
		}
		Globals.g_WcfRequestSummaryPresent = false;
		Globals.g_2and4frameworkSxsFailure = false;
		Globals.g_HttpRequestsQueued = false;
		Globals.g_ADODotNetConnectionsPresent = false;
		Globals.g_HttpContextThreads = null;
		Globals.g_WCFIOSchedulerThreads = null;
		Globals.g_CLRObjectOffSets = null;
		Globals.g_DumpStackObjects = null;
		Globals.g_ThreadWaitingOnSyncBlk = null;
		Globals.g_ThreadOwningSyncBlk = null;
		Globals.g_ThreadExceptionList = null;
		Globals.g_ManagedThreads = null;
		Globals.g_ThreadPoolIOThreads = null;
		Globals.g_ThreadPoolWorkerThreads = null;
		Globals.g_CLRAnalysisReport = null;
		Globals.g_ManagedDumpMTCache = null;
		Globals.g_ManagedIp2mdCache = null;
		Globals.g_WellKnownCLRExceptionsReported = null;
		Globals.g_SysNetConnectionWithBuffer = null;
		Globals.g_SysNetServicePointsWaitingOnSocket = null;
		Globals.g_CLRObjectOffSets = new Dictionary<double, Dictionary<string, string>>();
		Globals.g_ThreadWaitingOnSyncBlk = new Dictionary<int, object>();
		Globals.g_ThreadOwningSyncBlk = new Dictionary<object, string>();
		Globals.g_ThreadExceptionList = new Dictionary<int, string>();
		Globals.g_ManagedThreads = new Dictionary<string, string>();
		Globals.g_ThreadPoolWorkerThreads = new Dictionary<string, string>();
		Globals.g_ThreadPoolIOThreads = new Dictionary<string, string>();
		Globals.g_ManagedDumpMTCache = new Dictionary<string, string>();
		Globals.g_ManagedIp2mdCache = new Dictionary<string, string>();
		Globals.g_HttpRequestQueueTable = new StringBuilder();
		Globals.g_DumpStackObjects = new Dictionary<int, string>();
		Globals.g_HttpContextThreads = new Dictionary<double, int>();
		Globals.g_WCFIOSchedulerThreads = new Dictionary<double, int>();
		Globals.g_WellKnownCLRExceptionsReported = new Dictionary<string, string>();
		Globals.g_SysNetConnectionWithBuffer = new Dictionary<double, string>();
		Globals.g_SysNetServicePointsWaitingOnSocket = new Dictionary<string, int>();
		Globals.g_CLRAnalysisReport = new StringBuilder();
		Globals.g_webCache = false;
		Globals.g_haveObjectsReadyForFinalization = false;
	}

	public void ReportAllExceptions()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		string text = "";
		object obj = null;
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		object obj2 = null;
		string[] array = DumpAllExceptions();
		if (Globals.HelperFunctions.IsNullOrEmpty(array))
		{
			return;
		}
		Globals.g_ManagedExceptionsPresent = true;
		for (int i = 0; i <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); i++)
		{
			text = GetManagedExceptionType(array[i], bInnerException: false);
			text2 = Convert.ToString(DumpString(array[i], "_message"));
			if (text2 == Globals.NOT_FOUND || Globals.HelperFunctions.IsNullOrEmpty(text2))
			{
				text2 = "&lt;none&gt;";
			}
			obj = GetStackTraceFromException(array[i]);
			text = text + ";;" + text2 + ";;" + Convert.ToString(obj);
			if (!dictionary.ContainsKey(text))
			{
				dictionary.Add(text, 1);
			}
			else
			{
				dictionary[text]++;
			}
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("ManagedExceptionsInHeapsReport", (SectionType)0);
		val.Title = "Previous .NET Exceptions Report (Exceptions in all .NET Heaps)";
		val.Collapsible = true;
		val.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText><tr><th>Exception Type</th><th>&nbsp;&nbsp;&nbsp;Count&nbsp;&nbsp;&nbsp;</th><th>Message</th><th nowrap>&nbsp;&nbsp;&nbsp;Stack Trace</th></tr>");
		foreach (string key in dictionary.Keys)
		{
			string[] array2 = Globals.HelperFunctions.Split(key, ";;");
			text3 = array2[0];
			obj2 = dictionary[key];
			text4 = array2[1];
			if (text4 == Globals.NOT_FOUND)
			{
				text4 = "";
			}
			if (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1) >= 2)
			{
				text5 = array2[2];
				text5 = Convert.ToString(regexReplace(HTMLEncode(text5), "\\n", "<br>&nbsp;&nbsp;&nbsp;"));
			}
			CheckForWellKnownExceptionTypes(text3, text4, text5, null, obj2);
			val.Write("<tr><td><b>" + text3 + "</b></td><td>&nbsp;&nbsp;&nbsp;" + Convert.ToString(obj2) + "&nbsp;&nbsp;&nbsp;</td><td>" + text4 + "</td><td nowrap>&nbsp;&nbsp;&nbsp;" + Convert.ToString(text5) + "</td></tr>");
		}
		val.Write("</table>");
	}

	public string[] DumpAllExceptions()
	{
		List<string> list = new List<string>();
		foreach (ClrException item in Globals.g_Debugger.EnumerateHeapExceptionObjects())
		{
			list.Add($"{item.Address:x}");
		}
		return list.ToArray();
	}

	public void DisplayBangThreadPool()
	{
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("DOTNETTHREADPOOL", (SectionType)0);
		val.Title = ".NET ThreadPool Summary";
		ClrThreadPool threadPool = Globals.g_Debugger.ClrRuntime.GetThreadPool();
		if (threadPool == null)
		{
			val.Write("<i>The .NET ThreadPool is not initialized in this process</i>");
			return;
		}
		val.Write("<b>Worker Threads</b>");
		val.Write($"<div style='margin-left:20px'><table><tr><td>Total:</td><td>{threadPool.TotalThreads}</td></tr>");
		val.Write($"<tr><td>Running:</td><td>{threadPool.RunningThreads}</td></tr>");
		val.Write($"<tr><td>Idle:</td><td>{threadPool.IdleThreads}</td></tr>");
		val.Write($"<tr><td>Max:</td><td>{threadPool.MaxThreads}</td></tr>");
		val.Write($"<tr><td>Min:</td><td>{threadPool.MinThreads}</td></tr></table></div>");
		val.Write("<br><b>IO Threads</b>");
		val.Write($"<div style='margin-left:20px'><table><tr><td>Total:</td><td>{totalIOThreads}</td></tr>");
		val.Write($"<tr><td>Free:</td><td>{threadPool.FreeCompletionPortCount}</td></tr>");
		val.Write($"<tr><td>Max:</td><td>{threadPool.MaxCompletionPorts}</td></tr>");
		val.Write($"<tr><td>Max Free:</td><td>{threadPool.MaxFreeCompletionPorts}</td></tr>");
		val.Write($"<tr><td>Min:</td><td>{threadPool.MinCompletionPorts}</td></tr></table></div>");
	}

	public void DisplayBangThreads()
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected I4, but got Unknown
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("DOTNETTHREADS", (SectionType)0);
		val.Title = ".NET Threads Summary";
		_ = Globals.g_Debugger.ManagedThreads;
		val.Write("<table><tr align='center'><th width='100'>Debugger Thread ID</th><th width='100'>Managed Thread ID</th><th width='100'>OS Thread ID</th><th width='150'>Thread Object</th><th width='150'>GC Mode</th><th width='150'>Domain</th><th width='100'>Lock Count</th><th width='100'>Apt</th><th align='left'>Exception</th></tr>");
		foreach (ClrThread thread in Globals.g_Debugger.ClrRuntime.Threads)
		{
			string text = "Unknown";
			string text2 = ((thread.CurrentException == null) ? "" : thread.CurrentException.Type.Name);
			NetDbgThread threadByManagedThreadId = Globals.g_Debugger.GetThreadByManagedThreadId(thread.ManagedThreadId);
			string text3;
			if (threadByManagedThreadId != null)
			{
				COMApartmentType cOMApartmentType = threadByManagedThreadId.COMApartmentType;
				switch (cOMApartmentType - 1)
				{
				case 2:
					text = "STA";
					break;
				case 1:
					text = "MTA";
					break;
				case 0:
					text = "Neutral";
					break;
				default:
					if (thread.IsCoInitialized)
					{
						text = "Unknown";
					}
					break;
				}
				text3 = Globals.HelperFunctions.GetThreadIDWithLink(threadByManagedThreadId.ThreadID);
				if (string.IsNullOrEmpty(text2) && Convert.ToBoolean(Globals.g_ThreadExceptionList.ContainsKey(threadByManagedThreadId.ThreadID)))
				{
					string[] array = Strings.Split(Convert.ToString(Globals.g_ThreadExceptionList[threadByManagedThreadId.ThreadID]), ",");
					if (array.Length != 0)
					{
						string text4 = array[0];
						string text5 = DumpString(text4, "_className");
						if (text5 == Globals.NOT_FOUND)
						{
							text5 = GetManagedExceptionType(text4, bInnerException: false);
						}
						if (text5 != Globals.NOT_FOUND)
						{
							text2 = text5;
						}
					}
				}
				if (string.IsNullOrEmpty(text2))
				{
					if (thread.IsGC)
					{
						text2 = "(GC Thread)";
					}
					else if (thread.IsShutdownHelper)
					{
						text2 = "(Shutting Down Runtime)";
					}
					else if (thread.IsThreadpoolCompletionPort)
					{
						text2 = "(Threadpool Completion Port)";
						totalIOThreads++;
					}
					else if (thread.IsThreadpoolWorker)
					{
						string symbolFromAddress = Globals.g_Debugger.GetSymbolFromAddress(threadByManagedThreadId.StartAddress);
						text2 = ((!symbolFromAddress.Contains("ThreadpoolMgr::TimerThreadStart")) ? ((!symbolFromAddress.Contains("ThreadpoolMgr::WaitThreadStart")) ? "(Threadpool Worker)" : "(Threadpool Wait)") : "(Threadpool Timer)");
					}
					else if (thread.IsThreadpoolGate)
					{
						text2 = "(Threadpool Gate)";
					}
					else if (thread.IsThreadpoolTimer)
					{
						text2 = "(Threadpool Timer)";
					}
					else if (thread.IsThreadpoolWait)
					{
						text2 = "(Threadpool Wait)";
					}
					else if (thread.IsFinalizer)
					{
						text2 = "(Finalizer)";
					}
					else if (thread.IsDebuggerHelper)
					{
						text2 = "(Debugger Helper)";
					}
					else if (thread.IsSuspendingEE)
					{
						text2 = "(Suspending Runtime)";
					}
					else if (thread.IsUnstarted)
					{
						text2 = "(Unstarted)";
					}
					else if (thread.IsUserSuspended)
					{
						text2 = "(User Suspended)";
					}
				}
			}
			else
			{
				text = "";
				text3 = "";
			}
			string text6 = ((thread.OSThreadId == 0) ? "" : thread.OSThreadId.ToString());
			val.Write(string.Format("<tr align='center'><td width='100'>{0}</td><td width='100'>{1}</td><td width='100'>{2}</td><td width='150'>{3}</td><td width='150'>{4}</td><td width='150'>{5}</td><td width='100'>{6}</td><td width='100'>{7}</td><td align='left'>{8}</td></tr>", text3, thread.ManagedThreadId, text6, thread.Address.ToString("x"), ((int)thread.GcMode == 0) ? "Cooperative" : "Preemptive", thread.AppDomain.ToString("x"), thread.LockCount, text, text2));
		}
		val.Write("</table>");
	}

	public void CheckWebEngineQueue()
	{
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		CacheFunctions.ScriptModuleClass moduleByName = Globals.g_ModuleCache.GetModuleByName("webengine4");
		string dwordAtSymbolRaw;
		string dwordAtSymbolRaw2;
		string dwordAtSymbolRaw3;
		if (moduleByName != null)
		{
			dwordAtSymbolRaw = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine4!g_lActiveRequests");
			dwordAtSymbolRaw2 = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine4!W3_MGD_REQUEST_QUEUE::sm_lMaxActiveRequests");
			dwordAtSymbolRaw3 = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine4!W3_MGD_REQUEST_QUEUE::sm_EntryCount");
			if (dwordAtSymbolRaw3 != Globals.ERROR_BAD_SYMBOLS)
			{
				DisplayWebEngineQueueMessage(dwordAtSymbolRaw, dwordAtSymbolRaw2, Convert.ToInt32(dwordAtSymbolRaw3));
			}
			return;
		}
		moduleByName = Globals.g_ModuleCache.GetModuleByName("webengine");
		if (moduleByName == null)
		{
			return;
		}
		moduleByName.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
		if (Convert.ToInt32(Priv) < 1433)
		{
			return;
		}
		if (Convert.ToInt32(Priv) == 1433 || Convert.ToInt32(Priv) < 3053)
		{
			dwordAtSymbolRaw = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine!g_lActiveRequests");
			dwordAtSymbolRaw2 = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine!g_lMaxActiveRequests");
			DisplayWebEngineQueueMessage(dwordAtSymbolRaw, dwordAtSymbolRaw2, 0);
			return;
		}
		dwordAtSymbolRaw = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine!g_lActiveRequests");
		dwordAtSymbolRaw2 = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine!W3_MGD_REQUEST_QUEUE::sm_lMaxActiveRequests");
		dwordAtSymbolRaw3 = Globals.HelperFunctions.GetDwordAtSymbolRaw("webengine!W3_MGD_REQUEST_QUEUE::sm_EntryCount");
		if (dwordAtSymbolRaw3 != Globals.ERROR_BAD_SYMBOLS)
		{
			DisplayWebEngineQueueMessage(dwordAtSymbolRaw, dwordAtSymbolRaw2, Convert.ToInt32(dwordAtSymbolRaw3));
		}
	}

	public void DisplayWebEngineQueueMessage(string currentRequests, string MaxActiveRequests, int QueueCount)
	{
		long result = 0L;
		long result2 = 0L;
		long num = 0L;
		if (!(currentRequests != Globals.ERROR_BAD_SYMBOLS) || !(MaxActiveRequests != Globals.ERROR_BAD_SYMBOLS))
		{
			return;
		}
		num = Convert.ToInt64(QueueCount);
		if (!long.TryParse(currentRequests, out result) || !long.TryParse(MaxActiveRequests, out result2))
		{
			return;
		}
		result = Convert.ToInt64(currentRequests);
		result2 = Convert.ToInt64(MaxActiveRequests);
		num = Convert.ToInt64(QueueCount);
		if (result == result2 && result != 0L)
		{
			if (num > 0)
			{
				Globals.Manager.ReportError("In <b>" + Globals.g_ShortDumpFileName + "</b> <font color='red'><b>" + Convert.ToString(QueueCount) + " HTTP Requests</font></b> are queued in the <b>ASP.NET's webengine queue</b> (controlled by <b>MaxConcurrentRequestsPerCPU</b> setting) <br/><br/> Currently Executing Requests = " + Convert.ToString(currentRequests) + "<br/> Maximum allowed Active Requests = " + Convert.ToString(MaxActiveRequests) + "<br/>", "The requests start getting queued if all the threads available to process the request are busy. Look at the callstacks of the threads which are processing the request to see why they are running for a long time. <br/><br/> Also review the article <a href='http://blogs.msdn.com/b/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx'>ASP.NET Thread Usage on IIS 7.5, IIS 7.0, and IIS 6.0 </a> for more details on this issue. ", 0, "{d27be69f-52eb-47af-a0cc-bb0f659083c7}");
			}
			else
			{
				Globals.Manager.ReportError("In <b>" + Globals.g_ShortDumpFileName + "</b> the currently executing requests has reached the maximum executing request throttle <br/> Currently Executing Requests = " + Convert.ToString(currentRequests) + "<br/> Maximum allowed Active Requests = " + Convert.ToString(MaxActiveRequests) + "<br/>", "When the Currently Executing requests become equal to the maximum executing requests all the new requests start getting queued. Look at the callstack of the threads which are processing the requests to see why they are running for a long time. <br/><br/> Also review the article <a href='http://blogs.msdn.com/b/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx'>ASP.NET Thread Usage on IIS 7.5, IIS 7.0, and IIS 6.0 </a> for more details on this issue. ", 0, "{d27be69f-52eb-47af-a0cc-bb0f659083c7}");
			}
		}
	}

	public void CheckThreadPool()
	{
		//IL_07ca: Unknown result type (might be due to invalid IL or missing references)
		ClrThreadPool threadPool = Globals.g_Debugger.ClrRuntime.GetThreadPool();
		if (threadPool == null)
		{
			return;
		}
		int cpuUtilization = threadPool.CpuUtilization;
		string text = "";
		string text2 = "";
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string text3 = "";
		string text4 = threadPool.MaxThreads.ToString();
		string text5 = "";
		string text6 = Globals.g_Debugger.NumberProcessors.ToString();
		string text7 = "";
		string text8 = null;
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		object obj4 = null;
		string text9 = "";
		string text10 = "";
		string text11 = "";
		text = "";
		if (cpuUtilization > 50)
		{
			text = "In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + " the current <b>CPU Usage is " + cpuUtilization + "%</b> <BR><BR>";
			text2 = "Note this is the total % of CPU utilized for <b>all</b> processes on the machine. The particular process being analyzed (" + Path.GetFileName(Globals.g_Debugger.ExecutableName) + ") is not necessarily the culprit.<BR><BR>";
		}
		string[] array = DumpHeapForType("System.Web.Configuration.ProcessModelSection");
		if (array != null && array.Length != 0)
		{
			dynamic objectAt = GetObjectAt(array[0]);
			ClrObject val = (ClrObject)objectAt._values._entriesArray._items;
			if (val != null)
			{
				dynamic val2 = val;
				for (int i = 0; i < val.GetLength(); i++)
				{
					dynamic val3 = val2[i];
					if (val3.Key == "autoConfig")
					{
						dynamic val4 = val3.Value;
						text3 = ((!(bool)val4.Value) ? "false" : "true");
					}
				}
			}
		}
		string[] array2 = DumpHeapForType("System.Web.HttpRuntime");
		if (!Globals.HelperFunctions.IsNullOrEmpty(array2))
		{
			for (num = 0; num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1); num++)
			{
				text8 = DumpObject(array2[num], "_requestQueue");
				if (!(text8 != Globals.NOT_FOUND))
				{
					continue;
				}
				text5 = DumpShort(text8, "_minExternFreeThreads").ToString();
				text7 = DumpShort(text8, "_minLocalFreeThreads").ToString();
				int i = Convert.ToInt32(DumpShort(text8, "_count"));
				if (Convert.ToInt32(i) <= 0)
				{
					continue;
				}
				Globals.g_HttpRequestsQueued = true;
				object obj5 = DumpString(array2[num], "_appDomainAppId");
				obj = DumpObject(text8, "_externQueue");
				obj2 = DumpObject(text8, "_localQueue");
				obj3 = DumpShort(obj, "_size");
				obj4 = DumpShort(obj2, "_size");
				Globals.HelperFunctions.IsModuleLoaded("");
				if (!Globals.HelperFunctions.IsNullOrEmpty(text4))
				{
					text10 = "In order to work around this issue, set autoconfig=false and configure the threadpool settings in the machine.config processmodel and httpruntime tags. For more information on how to set these parameters, please refer to the following articles<br/><a href='http://support.microsoft.com/kb/821268'>Contention, poor performance, and deadlocks when you make Web service requests from ASP.NET applications</a><br/><a href='http://msdn.microsoft.com/en-us/library/ms998549#scalenetchapt06_topic8'>Improving ASP.NET Performance - Threading Explained</a>";
					text9 = "This is a known issue with Reporting Services if the version of SQL Server you are running does not support all the processors on your machine.<br/><br/> To find out the number of processors supported by the version of SQL server you are running, please refer to the following article.<br/><a href = 'http://msdn.microsoft.com/en-us/library/ms143760(v=sql.90).aspx'>Maximum Number of Processors Supported by the Editions of SQL Server</a>.<br><br/>" + text10;
					if (Convert.ToInt32(obj4) > 0)
					{
						if (Convert.ToInt32(DumpShort(text8, "_minLocalFreeThreads")) > Convert.ToInt32(text4) * Convert.ToInt32(text6))
						{
							if (Globals.HelperFunctions.IsModuleLoaded("ReportingServicesNativeServer") || Globals.HelperFunctions.IsModuleLoaded("ReportingServicesNativeClient"))
							{
								Globals.Manager.ReportError("All the requests corresponding to the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>" + Convert.ToString(obj5) + "</a> app domain will get queued as minLocalFreeThreads > MaxWorkerThreads ", text9, 0, "{2b530333-a7f9-428b-acbc-aa3813c6922e}");
							}
							else
							{
								Globals.Manager.ReportError("All the requests corresponding to the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>" + Convert.ToString(obj5) + "</a> app domain will get queued as minLocalFreeThreads > MaxWorkerThreads ", text10, 0, "{2b530333-a7f9-428b-acbc-aa3813c6922e}");
							}
						}
					}
					else if (Convert.ToInt32(obj3) > 0 && DumpShort(text8, "_minExternFreeThreads") > Convert.ToInt32(text4) * Convert.ToInt32(text6))
					{
						if (Globals.HelperFunctions.IsModuleLoaded("ReportingServicesNativeServer") || Globals.HelperFunctions.IsModuleLoaded("ReportingServicesNativeClient"))
						{
							Globals.Manager.ReportError("All the requests corresponding to the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>" + Convert.ToString(obj5) + "</a> app domain will get queued as minExternFreeThreads > MaxWorkerThreads ", text9, 0, "{2b530333-a7f9-428b-acbc-aa3813c6922e}");
						}
						else
						{
							Globals.Manager.ReportError("All the requests corresponding to the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>" + Convert.ToString(obj5) + "</a> app domain will get queued as minExternFreeThreads > MaxWorkerThreads ", text10, 0, "{2b530333-a7f9-428b-acbc-aa3813c6922e}");
						}
					}
				}
				stringBuilder.Append("<tr><td>" + Convert.ToString(obj5) + "</td><td>" + Convert.ToString(obj3) + "</td><td>" + Convert.ToString(obj4) + "</td></tr>");
			}
		}
		text11 = "";
		if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) >= 2 && Convert.ToBoolean(Globals.g_IsSystemWebApp))
		{
			if (text3 == "false")
			{
				text += "ThreadPool autoConfig is set to false";
				text2 += "In .NET 2.0 and later, autoConfig is True by default to achieve optimal performance based on the machine configuration.<br> Please either use autoConfig = 'true', or review <a href='http://support.microsoft.com/?id=821268'>KB821268</a> before using any custom settings<br>";
				if (Convert.ToBoolean(Globals.g_IsIIS7))
				{
					text2 += "Please also read this <a href='http://blogs.msdn.com/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx'>http://blogs.msdn.com/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx</a> for IIS7-specific information.";
				}
			}
			text11 = "<br><br><b> ASP.NET Threadpool settings</b><table border=0 cellpadding=1 cellspacing=3 class=myCustomText><tr><td>maxWorkerThreads</td><td>" + text4 + "</td></tr><tr><td>minFreeThreads</td><td>" + text5 + "</td></tr><tr><td>minLocalRequestFreeThreads</td><td>" + text7 + "</td></tr><tr><td>Number of Processors</td><td>" + text6 + "</td></tr></table>";
		}
		if (Globals.g_HttpRequestsQueued)
		{
			Globals.g_HttpRequestQueueTable.Append("<table border=0 cellpadding=1 cellspacing=5 class=myCustomText><tr><td><b> <tr><td><b>AppDomain</b></td><td><b>External Queue</b></td><td><b>Local Queue</b></td></tr>");
			Globals.g_HttpRequestQueueTable.Append(stringBuilder.ToString());
			Globals.g_HttpRequestQueueTable.Append("</table>");
			ReportSection val5 = Globals.Manager.CurrentSection.AddChildSection("REQUESTQUEUE", (SectionType)0);
			val5.Title = "ASP.NET Request Queue Report";
			val5.Collapsible = true;
			val5.Write(Globals.g_HttpRequestQueueTable.ToString());
			if (text11 != "")
			{
				val5.Write(text11);
				val5.Write("<br><br><div style='width:1200;class=myCustomText'>A request is queued in the local queue if the request is local to the web server and a request gets queued in the extern queue if the request is coming from a remote machine. At any point of time, ASP.NET never executes requests more than <B>(maxWorkerThreads - minFreeThreads) * Number of logical processors</B>. The setting that impacts the requests getting queued in the local queue is <b>minLocalRequestFreeThreads</b>. minLocalRequestFreeThreads is used by the worker process to queue requests from localhost (where a Web application sends requests to a local Web service) if the number of available threads in the thread pool falls below this number. This setting is similar to minFreeThreads but it only applies to localhost requests from the local computer</div><br>");
			}
			if (text11 != "")
			{
				Globals.Manager.ReportError("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> HTTP Requests are getting queued.  <br/><br/>", "The requests start getting queued if all the threads available to process the request are busy. Look at the callstacks of the threads which are processing the request to see why they are running for a long time. Also, look at the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>ASP.NET Request Queue Report</a> to see the ASP.NET Threadpool settings and the runtime for which the requests are getting queued.", 0, "{87750075-0e80-4ddc-a4f1-de1d702ab80c}");
			}
			else
			{
				Globals.Manager.ReportError("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> HTTP Requests are getting queued.  <br/><br/>", "The requests start getting queued if all the threads available to process the request are busy. Look at the callstacks of the threads which are processing the request to see why they are running for a long time. Also, look at the <a href='#REQUESTQUEUE" + Convert.ToString(Globals.g_UniqueReference) + "'>ASP.NET Request Queue Report</a> to see the runtime for which the requests are getting queued.", 0, "{87750075-0e80-4ddc-a4f1-de1d702ab80c}");
			}
		}
		if (text != "")
		{
			Globals.Manager.ReportWarning(text, text2, 0, "{1291d9da-ba9b-40a0-86e7-def9ba6c5cac}");
		}
	}

	public void AnalyzeBlockedFinalizer()
	{
		string text = "";
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		Globals.g_FinalizerThreadBlocked = false;
		text = "";
		if (Globals.g_FinalizerThreadId > -1)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(Globals.g_FinalizerThreadId);
		}
		Globals.HelperFunctions.ResetStatusNoIncrement("Checking for blocked Finalizer Thread");
		if (Convert.ToInt32(Globals.g_FinalizerThreadId) != -1 && Globals.g_FinalizerThreadId > -1)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(Globals.g_FinalizerThreadId);
			if (scriptThreadClass.FindFrameInStack("FINALIZERTHREADSTART") > -1)
			{
				if (scriptThreadClass.FindFrameInStack("WAITFORFINALIZEREVENT") > -1)
				{
					Globals.g_FinalizerThreadBlocked = false;
				}
				else if (scriptThreadClass.FindFrameInStack("WAITUNTILGCCOMPLETE") > -1 || scriptThreadClass.FindFrameInStack("WAIT_FOR_GC_DONE") > -1)
				{
					Globals.g_FinalizerThreadBlocked = false;
				}
				else
				{
					Globals.g_FinalizerThreadBlocked = true;
					if (Globals.g_AnalyzedThreads != null && Convert.ToBoolean(Globals.g_AnalyzedThreads.Exists(Globals.g_FinalizerThreadId)) && (Convert.ToString(Globals.g_AnalyzedThreads.Item(Globals.g_FinalizerThreadId).Category).StartsWith("COMCALLSTA") || (Convert.ToInt32(Globals.g_SizeOfULongPtr) == 8 && scriptThreadClass.FindFrameInStack("GETTOSTA") > -1)))
					{
						text = "The finalizer thread is probably tring to release an instance of an STA COM component, and it is stuck waiting for the STA thread to become available. To resolve this problem, call <a href='http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.marshal.releasecomobject.aspx'>Marshal.ReleaseComObject</a> to clean up the references for all apartment-threaded COM components that you have used in your code deterministically (immediately), rather than waiting for the Finalizer Thread to do it at a later time.";
						if (scriptThreadClass != null && scriptThreadClass.COMDestinationProcessID == Globals.g_Debugger.ProcessID && Convert.ToBoolean(Globals.g_ThreadPoolWorkerThreads.ContainsKey(Convert.ToString(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(scriptThreadClass.COMDestinationThreadID)))) && Convert.ToBoolean(IsLowerRuntimeVersion(238)))
						{
							text += "<br><br>Note there is a known issue in .net framework 2.0 RTM version which can contribute to this problem. To ensure that you are not running in to the same issue, install the latest service pack for .net framework 2.0. <br><br> More details on this issue can be found in the <a href='http://support.microsoft.com/kb/928569'>KB928569</a> and the blog <a href='http://blogs.msdn.com/tess/archive/2008/06/12/asp-net-case-study-deadlock-waiting-in-gettosta.aspx'>ASP.NET Case Study: Deadlock waiting in GetToSTA</a>";
						}
					}
				}
			}
		}
		if (text == "")
		{
			text = "Review the callstack for the Finalizer thread to see what the finalizer is blocked on. Long running code on a Finalizer thread can increase the number of objects ready for finalization and is bad for the overall memory consumption of the process";
		}
		if (Globals.g_FinalizerThreadBlocked)
		{
			Globals.Manager.ReportError("The finalizer thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(Globals.g_FinalizerThreadId))) + " in this " + Convert.ToString(Globals.g_ShortDumpFileName) + " is blocked", text, 0, "{aea42889-3ede-41a2-8737-3423b52752cc}");
		}
	}

	public void FindSyncBlk()
	{
		ClrHeap clrHeap = Globals.g_Debugger.ClrHeap;
		if (clrHeap == null || !clrHeap.CanWalkHeap)
		{
			return;
		}
		IEnumerable<BlockingObject> enumerable = from sb in clrHeap.EnumerateBlockingObjects()
			where (int)sb.Reason == 2 && sb.Waiters.Count > 0
			select sb;
		Globals.HelperFunctions.ResetStatus("Finding all threads waiting on .NET locks", enumerable.Count(), "Lock");
		foreach (BlockingObject item in enumerable)
		{
			ClrThread val = item.Owners[0];
			if (item.Owners.Count > 0 && val != null && val.OSThreadId != 0)
			{
				string text = Globals.g_Debugger.GetThreadBySystemID((int)val.OSThreadId).ThreadID.ToString();
				Globals.g_ThreadOwningSyncBlk.Add(item, text);
				if (item.Waiters.Count > 0 && !Globals.g_ThreadsWithSyncBlkWaiters.Contains(text))
				{
					Globals.g_ThreadsWithSyncBlkWaiters.Add(text);
				}
				foreach (ClrThread waiter in item.Waiters)
				{
					Globals.g_ThreadWaitingOnSyncBlk.Add(Globals.g_Debugger.GetThreadBySystemID((int)waiter.OSThreadId).ThreadID, item);
				}
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public string IsWaitingOnSyncblk(int threadid)
	{
		string text = "";
		if (Globals.g_ThreadWaitingOnSyncBlk.ContainsKey(threadid))
		{
			return FindOwnerForSyncblk(Globals.g_ThreadWaitingOnSyncBlk[threadid]);
		}
		return "";
	}

	public string FindOwnerForSyncblk(object syncBlk)
	{
		string text = "";
		if (Globals.g_ThreadOwningSyncBlk.ContainsKey(syncBlk))
		{
			return Globals.g_ThreadOwningSyncBlk[syncBlk];
		}
		return "";
	}

	public string GetTEB(CacheFunctions.ScriptThreadClass Thread)
	{
		GetTebAsHex(Thread);
		return "";
	}

	public string GetTebAsHex(CacheFunctions.ScriptThreadClass Thread)
	{
		string text = Globals.HelperFunctions.DebuggerExecuteReplaceLF("~" + Thread.ThreadID + "e r $teb", "");
		if (Globals.HelperFunctions.Left(text.ToLower(), 5) == "$teb=")
		{
			text = Globals.HelperFunctions.Right(text, Globals.g_SizeOfULongPtr * 2);
		}
		return text;
	}

	public bool IsWebApp()
	{
		bool flag = false;
		flag = false;
		flag = Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("System_Web_ni"));
		if (!flag)
		{
			flag = Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("System_Web"));
		}
		return flag;
	}

	public bool IsWcfServiceHost()
	{
		if (Globals.g_ModuleCache.GetModuleByName("System_ServiceModel_ni") == null && Globals.g_ModuleCache.GetModuleByName("System_ServiceModel") == null)
		{
			return false;
		}
		return Globals.g_Debugger.EnumerateHeapObjects().Any((ClrObject w) => ClrHelper.IsSubclassOf(w.GetHeapType(), "System.ServiceModel.ServiceHostBase"));
	}

	public bool IsWcfClient()
	{
		if (Globals.g_ModuleCache.GetModuleByName("System_ServiceModel_ni") == null && Globals.g_ModuleCache.GetModuleByName("System_ServiceModel") == null)
		{
			return false;
		}
		return Globals.g_Debugger.EnumerateHeapObjects().Any((ClrObject w) => ClrHelper.IsSubclassOf(w.GetHeapType(), "System.ServiceModel.Channels.TransportOutputChannel") || ClrHelper.Is(w, "System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel") || ClrHelper.Is(w, "System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel<System.ServiceModel.Channels.IRequestChannel>"));
	}

	public bool IsIIS7App()
	{
		bool flag = false;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		flag = false;
		scriptModuleClass = Globals.g_ModuleCache.GetModuleByName("w3wp");
		if (scriptModuleClass == null)
		{
			return flag;
		}
		scriptModuleClass.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
		if (Convert.ToInt32(Major) == 7)
		{
			flag = true;
		}
		return flag;
	}

	public string GetManagedExceptionType(object ExceptionObjHexAddr, bool bInnerException)
	{
		string result = null;
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			if (bInnerException)
			{
				result = GetManagedExceptionType(DumpObject(ExceptionObjHexAddr, "_innerException"), bInnerException: false);
			}
			else
			{
				ClrObject objectAt = GetObjectAt(ExceptionObjHexAddr);
				if (objectAt != null)
				{
					ClrException exceptionObject = Globals.g_Debugger.ClrHeap.GetExceptionObject(objectAt.GetValue());
					result = ((exceptionObject == null || exceptionObject.Type == null || !(exceptionObject.Type.Name.ToUpper() != "<UNKNOWN>")) ? ((string)getManagedObjectType(ExceptionObjHexAddr)) : exceptionObject.Type.Name);
				}
			}
		}
		return result;
	}

	public string GetManagedExceptionMsg(object ExceptionObjHexAddr, bool bInnerException)
	{
		string result = null;
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			if (bInnerException)
			{
				result = GetManagedExceptionMsg(DumpObject(ExceptionObjHexAddr, "_innerException"), bInnerException: false);
			}
			else
			{
				try
				{
					ClrObject objectAt = GetObjectAt(ExceptionObjHexAddr);
					ClrException exceptionObject = Globals.g_Debugger.ClrHeap.GetExceptionObject(objectAt.GetValue());
					if (exceptionObject != null)
					{
						result = exceptionObject.Message;
					}
				}
				catch
				{
				}
			}
		}
		return result;
	}

	public object getManagedObjectType(object objHexAddr)
	{
		ClrObject objectAt = GetObjectAt(objHexAddr);
		if (objectAt == null)
		{
			return Globals.NOT_FOUND;
		}
		return objectAt.GetHeapType().Name;
	}

	public object GetManagedIp2md(string ipValInHex)
	{
		return Globals.g_Debugger.GetSymbolFromAddress(Globals.HelperFunctions.FromHex(ipValInHex));
	}

	public void PrintContextHeader()
	{
		Globals.Manager.Write("<table border=0 cellpadding=1 cellspacing=5 class=myCustomText><tr><td><b>");
		Globals.Manager.Write("HttpContext</b></td><td><b>Timeout</b></td><td><b>Completed</b></td><td><b>RunningSince</b></td><td><b>ThreadId</b></td><td><b>ReturnCode</b></td><td><b>Verb</b></td><td><b>RequestPath+QueryString</b></td></tr>");
	}

	public void PrintContextFooter()
	{
		Globals.Manager.Write("</table> <br>");
	}

	public DateTime ConvertUnixTime(uint seconds)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(seconds);
	}

	public void PrintHttpContextInfo(object context, bool boolHeader)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		double num = 0.0;
		object obj4 = null;
		object obj5 = null;
		object obj6 = null;
		string text2 = "";
		object obj7 = null;
		object obj8 = null;
		string text3 = "";
		dynamic val = null;
		ulong num2 = 0uL;
		ulong num3 = 0uL;
		uint seconds = 0u;
		((IDebugControl2)Globals.g_Debugger.RawDebugger).GetCurrentTimeDate(ref seconds);
		DateTime dateTime = ConvertUnixTime(seconds);
		if (boolHeader)
		{
			PrintContextHeader();
		}
		obj2 = DumpObject(context, "_request");
		obj3 = DumpObject(context, "_response");
		val = GetObjectAt(context);
		ClrInstanceField fieldByName = ((ClrObject)val).GetHeapType().GetFieldByName("_timeout");
		_ = Globals.g_Debugger.ClrVersionInfo.Version;
		if (fieldByName == null)
		{
			num = TimeSpan.FromTicks(Convert.ToInt64((string)DumpQuad(context, "_timeoutTicks"), 16)).TotalSeconds;
		}
		else
		{
			num3 = val._timeout;
			num2 = Convert.ToUInt64(Globals.g_Debugger.ReadQWord(Convert.ToDouble(num3)));
			num = new TimeSpan(Convert.ToInt64(num2)).TotalSeconds;
		}
		obj6 = DumpString(obj2, "_httpMethod");
		text2 = Convert.ToString(DumpString(obj2, "_queryStringText"));
		if (text2 == Convert.ToString(Globals.NOT_FOUND))
		{
			text2 = "";
		}
		obj7 = DumpObject(obj2, "_path");
		if ((string)obj7 == Globals.NOT_FOUND)
		{
			obj7 = DumpObject(obj2, "_filePath");
		}
		obj8 = DumpString(obj7, "_virtualPath");
		obj5 = DumpLong(obj3, "_statusCode");
		obj4 = (IsHttpRequestCompleted(context) ? "1" : "0");
		if (Convert.ToInt32(obj4) == 0)
		{
			obj4 = "No";
			DateTime dateTimeFromAddress = GetDateTimeFromAddress((ulong)val._utcTimestamp);
			obj = Math.Round((dateTime - dateTimeFromAddress).TotalSeconds);
			if (Information.IsNumeric(obj) && Convert.ToInt32(obj) > 60)
			{
				Globals.g_threadCountManagedRunningMoreThan60Secs++;
			}
			text = GetThreadForHttpContext(context);
			if (text != "XXX" && Convert.ToString(text) != "0")
			{
				Globals.g_HttpContextThreads.Add(Globals.HelperFunctions.FromHex((string)context), Convert.ToInt32(text));
			}
			obj = Convert.ToString(obj) + " Sec";
		}
		else
		{
			obj4 = "Yes";
			text = "XXX";
		}
		text3 = ((!(text != "XXX")) ? "---" : Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text))));
		Globals.Manager.Write("<tr><td align='right'>" + Convert.ToString(context) + "</td><td align='right'> " + Convert.ToString(num) + " Sec </td><td align='right'>" + Convert.ToString(obj4) + "</td><td align='right'>" + Convert.ToString(obj) + "</td>");
		Globals.Manager.Write("<td align='right'>" + text3 + "</td><td align='center'>" + Convert.ToString(obj5) + "</td><td>" + Convert.ToString(obj6) + "</td><td>" + Convert.ToString(obj8) + " " + Convert.ToString(HTMLEncode(text2) + "</td></tr>"));
		if (boolHeader)
		{
			PrintContextFooter();
		}
	}

	private bool IsHttpRequestCompleted(object context)
	{
		dynamic objectAt = GetObjectAt(context);
		bool flag = (bool)objectAt._response._completed;
		if (flag)
		{
			return flag;
		}
		ClrInstanceField fieldByName = ((ClrType)objectAt.GetHeapType()).GetFieldByName("_finishPipelineRequestCalled");
		if (fieldByName != null)
		{
			flag = (bool)fieldByName.GetFieldValue(objectAt.GetValue());
		}
		if (!flag)
		{
			dynamic val = objectAt._appInstance._stepManager;
			if (val != null && !val.IsNull())
			{
				flag = (bool)val._requestCompleted;
			}
		}
		return flag;
	}

	public string GetThreadForHttpContext(object context)
	{
		string result = "XXX";
		dynamic objectAt = GetObjectAt(context);
		if ((object)(ClrField)objectAt.GetHeapType().GetFieldByName("_thread") == null)
		{
			return result;
		}
		if (objectAt._thread == null)
		{
			return result;
		}
		ulong threadAddr = (ulong)objectAt._thread.DONT_USE_InternalThread.GetValue();
		if (threadAddr == 0L)
		{
			return result;
		}
		IEnumerable<ClrThread> source = Globals.g_Debugger.ClrRuntime.Threads.Where((ClrThread thread) => thread.Address == threadAddr);
		new List<ulong>(((IEnumerable<ClrThread>)Globals.g_Debugger.ClrRuntime.Threads).Select((Func<ClrThread, ulong>)((ClrThread thread) => (uint)thread.Address)));
		ClrThread val = source.FirstOrDefault();
		if (val != null)
		{
			result = Globals.g_Debugger.GetThreadBySystemID((int)val.OSThreadId).ThreadID.ToString();
		}
		return result;
	}

	public void PrintHttpContextInformation()
	{
		int num = 0;
		int num2 = 0;
		Array array = null;
		string text = "";
		array = DumpHeapForType("System.Web.HttpContext");
		if (Globals.HelperFunctions.IsNullOrEmpty(array) || Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1) < 0)
		{
			return;
		}
		Globals.g_HttpContextsPresent = true;
		ReportSection currentSection = Globals.Manager.CurrentSection;
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("HttpContextReport", (SectionType)0);
		val.Title = "HttpContext Report";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		text = "<PRE>";
		PrintContextHeader();
		num = 0;
		for (num2 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num2 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num2++)
		{
			if (GetObjectAt(array.GetValue(num2)).GetHeapType().Name == "System.Web.HttpContext")
			{
				PrintHttpContextInfo(array.GetValue(num2), boolHeader: false);
				num++;
			}
		}
		PrintContextFooter();
		val.Write("Total " + Convert.ToString(num) + " HttpContext objects");
		val.Write(text);
		val.Write("</pre>");
		Globals.Manager.CurrentSection = currentSection;
	}

	public string regexReplace(object aSrcStr, string aPattern, string aReplacement)
	{
		return new Regex(aPattern, RegexOptions.None).Replace(Convert.ToString(aSrcStr), aReplacement);
	}

	public void GetGarbageCollectionInformation()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		int num = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		flag = false;
		if (Globals.g_clrModule == null)
		{
			return;
		}
		if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) >= 2 && Convert.ToString(Globals.g_clrModule.ModuleName).ToUpper() != "MSCORSVR")
		{
			if (Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors(Convert.ToString(Globals.g_clrModule.ModuleName) + "!WKS::gc_heap::gc_started")) == 1)
			{
				flag = true;
			}
			else if (Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors(Convert.ToString(Globals.g_clrModule.ModuleName) + "!SVR::gc_heap::gc_started")) == 1)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			return;
		}
		if (Globals.g_GCThread == -1)
		{
			for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
			{
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
				for (int i = 0; i <= Convert.ToInt32(scriptThreadClass.StackFrames.Count) - 1; i++)
				{
					if (Convert.ToString((object)CacheFunctions.GetFunctionName(scriptThreadClass.StackFrames[i].InstructionAddress)).IndexOf("GARBAGECOLLECTGENERATION") >= 0)
					{
						Globals.g_GCThread = scriptThreadClass.ThreadID;
						break;
					}
				}
			}
			if (Globals.g_GCThread != -1)
			{
				Globals.Manager.ReportError("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> GC is running in this process. The Thread that triggered the GC is " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(Globals.g_GCThread))), "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details", 0, "{45f4b099-f4f5-4041-bc92-ad749801a73a}");
			}
			else
			{
				Globals.Manager.ReportError("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> GC is running in this process. The current set of scripts were not able to determine which thread induced GC", "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details", 0, "{45f4b099-f4f5-4041-bc92-ad749801a73a}");
			}
		}
		else
		{
			Globals.Manager.ReportError("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> GC is running in this process. The Thread that triggered the GC is " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(Globals.g_GCThread))), "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details", 0, "{45f4b099-f4f5-4041-bc92-ad749801a73a}");
		}
	}

	public void FindCLRKnownThreads()
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		object obj = null;
		if (Globals.g_Debugger.ClrRuntime == null || Globals.g_Debugger.ClrRuntime.Threads == null)
		{
			return;
		}
		foreach (ClrThread thread in Globals.g_Debugger.ClrRuntime.Threads)
		{
			NetDbgThread threadByManagedThreadId = Globals.g_Debugger.GetThreadByManagedThreadId(thread.ManagedThreadId);
			if (threadByManagedThreadId == null)
			{
				continue;
			}
			Globals.g_ManagedThreads.Add(thread.Address.ToString("X").ToUpper(), thread.OSThreadId.ToString());
			if (thread.IsSuspendingEE)
			{
				Globals.g_GCThread = threadByManagedThreadId.ThreadID;
			}
			else if (thread.IsFinalizer)
			{
				Globals.g_FinalizerThreadId = Convert.ToInt32(threadByManagedThreadId.ThreadID);
			}
			else if (thread.IsThreadpoolWorker)
			{
				Globals.g_ThreadPoolWorkerThreads.Add(threadByManagedThreadId.ThreadID.ToString(), thread.OSThreadId.ToString());
			}
			else if (thread.IsThreadpoolCompletionPort)
			{
				Globals.g_ThreadPoolIOThreads.Add(threadByManagedThreadId.ThreadID.ToString(), thread.OSThreadId.ToString());
			}
			if ((int)thread.GcMode == 0 && !thread.IsGC)
			{
				num = Convert.ToInt32(threadByManagedThreadId.ThreadID);
				Globals.g_PreemptiveGCDisabledThreads = Globals.g_PreemptiveGCDisabledThreads + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(num)) + ",";
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
				obj = scriptThreadClass.ClrStackReportNoArgs;
				if (Convert.ToString(obj).IndexOf("System.Xml.Schema.XmlSchemaSet.Add") >= 0)
				{
					Globals.Manager.ReportWarning("There is a known issue in the .NET framework where calls to System.Xml.Schema.XmlSchemaSet.Add() on threads which pre-emptive GC disabled can result in hang due to GC Deadlocks", "Please refer to <a href='http://blogs.msdn.com/b/tess/archive/2008/02/11/hang-caused-by-gc-xml-deadlock.aspx'>Hang caused by GC - XML Deadlock<a> for more details on this issue.", 0, "{0a327ede-b9a9-43b5-a923-710e613e6070}");
				}
				else if (Convert.ToString(obj).IndexOf("System.Reflection.Assembly.LoadFile") >= 0 && scriptThreadClass.FindFrameInStack("ASSEMBLYNATIVE::LOADFILE") > -1 && scriptThreadClass.FindFrameInStack("WAITFORSINGLEOBJECTEX") > -1)
				{
					Globals.Manager.ReportWarning("When two threads call a fusion API to access the same assembly, a deadlock may occur between the two threads in the Microsoft .NET Framework 2.0.", "Please refer to <a href='http://blogs.msdn.com/b/tess/archive/2009/09/22/asp-net-case-study-hang-when-loading-assemblies.aspx'>ASP.NET Case Study: Hang when loading assemblies<a> for more details on this issue.", 0, "{44a1e568-af48-4a92-a16b-0340a2893232}");
				}
			}
			if (thread.CurrentException != null && Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) >= 2)
			{
				AddExceptionsFromBangThreads(Convert.ToInt32(threadByManagedThreadId.ThreadID), thread.CurrentException.Address.ToString("X"));
			}
		}
	}

	public void AddExceptionsFromBangThreads(int intThreadId, string exceptionObject)
	{
		if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_ThreadExceptionList.ContainsKey(intThreadId))))
		{
			Globals.g_ThreadExceptionList.Add(intThreadId, exceptionObject);
		}
		else if (Convert.ToString(Globals.g_ThreadExceptionList[intThreadId]).IndexOf(exceptionObject) < 0)
		{
			Globals.g_ThreadExceptionList[intThreadId] = Convert.ToString(Globals.g_ThreadExceptionList[intThreadId]) + "," + exceptionObject;
		}
	}

	public void FindDebugTrue()
	{
		string[] array = null;
		string text = null;
		string[] array2 = null;
		string text2 = "";
		array = DumpHeapForType("System.Web.HttpRuntime");
		if (Globals.HelperFunctions.IsNullOrEmpty(array))
		{
			return;
		}
		array2 = DumpHeapForType("System.Web.Configuration.DeploymentSection");
		if (!Globals.HelperFunctions.IsNullOrEmpty(array2))
		{
			IsStaticBoolTrueInAnyAppDomain(array2[0], "s_retail");
		}
		text2 = "";
		string[] array3 = array;
		foreach (string text3 in array3)
		{
			if (Convert.ToInt32(DumpByte(text3, "_debuggingEnabled")) == 1)
			{
				text = DumpString(text3.Trim(), "_appDomainAppPath");
				text2 = text2 + "<div><b>" + text + "</b></div>\n";
			}
			if (Convert.ToString(DumpByte(text3, "_shutdownInProgress")) == "1")
			{
				AnalyzeShuttingHttpRuntime(text3.Trim());
			}
		}
		if (!string.IsNullOrEmpty(text2))
		{
			Globals.Manager.ReportInformation("In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> debug is set to true for the runtime <div>" + text2 + "</div><br>For applications running in production Debug should never be set to true and it has great performance impact. For more details on debug setting please refer to this blog <a href='http://blogs.msdn.com/tess/archive/2006/04/13/575364.aspx'>If your application is in production then why is debug=true</a>", -1, "{edfdde0e-c4b5-47db-ad47-b48027ebdd2a}");
		}
	}

	public bool IsStaticBoolTrueInAnyAppDomain(string objectAddress, string variable)
	{
		foreach (ClrAppDomain appDomain in Globals.g_Debugger.ClrRuntime.AppDomains)
		{
			ClrObject objectAt = GetObjectAt(objectAddress);
			if (objectAt == null || objectAt.IsNull())
			{
				continue;
			}
			foreach (ClrStaticField staticField in objectAt.GetHeapType().StaticFields)
			{
				if (((ClrField)staticField).Name == variable)
				{
					object fieldValue = staticField.GetFieldValue(appDomain);
					if (fieldValue is bool && (bool)fieldValue)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public void AnalyzeShuttingHttpRuntime(object httpRunTime)
	{
		bool flag = false;
		string text = "";
		string text2 = "";
		object obj = null;
		string text3 = "";
		string text4 = "";
		int num = 0;
		text2 = DumpString(httpRunTime, "_shutDownMessage");
		text = DumpString(httpRunTime, "_shutDownStack");
		switch (Convert.ToInt32(DumpLong(httpRunTime, "_shutdownReason")))
		{
		case 0:
			text4 = "None - No shutdown reason was provided";
			break;
		case 1:
			text4 = "HostingEnvironment - The hosting environment shut down the application domain";
			break;
		case 2:
			text4 = "ChangeInGlobalAsax - A change was made to the Global.asax file";
			flag = true;
			break;
		case 3:
			text4 = "ConfigurationChange - A change was made to the application-level configuration file";
			flag = true;
			break;
		case 4:
			text4 = "UnloadAppDomainCalled - A call was made to UnloadAppDomain";
			break;
		case 5:
			text4 = "ChangeInSecurityPolicyFile - A change was made in the code access security policy file";
			flag = true;
			break;
		case 6:
			text4 = "BinDirChangeOrDirectoryRename - A change was made to the Bin folder or to files in it";
			flag = true;
			break;
		case 7:
			text4 = "BrowsersDirChangeOrDirectoryRename - A change was made to the App_Browsers folder or to files in it";
			flag = true;
			break;
		case 8:
			text4 = "CodeDirChangeOrDirectoryRename - A change was made to the App_Code folder or to files in it";
			flag = true;
			break;
		case 9:
			text4 = "ResourcesDirChangeOrDirectoryRename - A change was made to the App_GlobalResources folder or to files in it";
			flag = true;
			break;
		case 10:
			text4 = "IdleTimeout - The maximum idle time limit was reached";
			break;
		case 11:
			text4 = "PhysicalApplicationPathChanged - A change was made to the physical path of the application";
			break;
		case 12:
			text4 = "HttpRuntimeClose - A call was made to Close";
			break;
		case 13:
			text4 = "InitializationError - An AppDomain initialization error occurred";
			break;
		case 14:
			text4 = "MaxRecompilationsReached - The maximum number of dynamic recompiles of resources was reached";
			break;
		case 15:
			text4 = "BuildManagerChange - The compilation system shut the application domain";
			break;
		default:
			text4 = "UNKNOWN - The current set of scripts were not able to determine the cause of AppDomain Shutdown";
			break;
		}
		obj = DumpString(httpRunTime, "_appDomainAppId");
		text3 = "See the <a href='#HttpRuntimeShutdownReport" + Globals.g_UniqueReference + "'>HttpRuntime Shutdown Report</a> to view more details of this specific shutdown event<br><br>Please refer to the article <a href='http://blogs.msdn.com/b/tess/archive/2006/08/02/686373.aspx'>Lost session variables and appdomain recycles</a> for more details on this issue in general.";
		string text5 = "In <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>, the HttpRuntime for the application <b>" + Convert.ToString(obj) + "</b> is in the middle of a shutdown.";
		Globals.Manager.ReportError(text5, text3, 0, "{92297e2a-c9a6-4315-9339-76b65480b559}");
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("HttpRuntimeShutdownReport", (SectionType)0);
		val.Title = "HttpRuntime Shutdown Report";
		val.Collapsible = true;
		string text6 = "<b><u>ShutDown Reason</u></b><br/><br/>" + text4;
		if (flag)
		{
			string webFileMonitorDetails = GetWebFileMonitorDetails();
			if (!string.IsNullOrEmpty(webFileMonitorDetails))
			{
				text6 = text6 + "<br/><br/> <b><u>Modified Files</u></b><br/>" + webFileMonitorDetails;
			}
		}
		text = Strings.Replace(text, "\n", "<br/>");
		text2 = Strings.Replace(text2, "\n", "<br/>");
		text6 = text6 + "<br/><br/> <b><u>Message</u></b><br/><br/>" + text2 + "<br/><br/><b><u>ShutDownStack </u></b><br/><br/>" + text;
		val.Write(text6);
	}

	private string GetWebFileMonitorDetails()
	{
		string text = string.Empty;
		foreach (dynamic item in Globals.g_Debugger.EnumerateHeapObjects("System.Web.FileMonitor"))
		{
			if ((int)item._lastAction == 3)
			{
				text += $"<BR>FileName: {(string)item.DirectoryMonitor.Directory}\\{(string)item._fileNameLong}<BR>";
				text += $"Modified: {GetDateTimeFromAddress((ulong)item._utcLastCompletion)}<BR>";
			}
		}
		return text;
	}

	private DateTime GetDateTimeFromAddress(ulong address)
	{
		return new DateTime(Convert.ToInt64(Convert.ToUInt64(Globals.g_Debugger.ReadQWord(Convert.ToDouble(address))) & 0x3FFFFFFFFFFFFFFFL));
	}

	public bool WildCardMatchTypeName(object inputStr, object typeString)
	{
		bool flag = false;
		flag = false;
		new Regex(Strings.Replace(Convert.ToString(inputStr), "*", "[\\w]*") + "$", RegexOptions.None);
		if (Regex.Matches(Convert.ToString(typeString), null).Count > 0)
		{
			flag = true;
		}
		return flag;
	}

	public string[] DumpHeapForType(string ObjectType)
	{
		List<string> list = new List<string>();
		foreach (ulong item in Globals.g_Debugger.EnumerateHeap(ObjectType))
		{
			list.Add(item.ToString("x"));
		}
		return list.ToArray();
	}

	public string normalizeWhitespace(string stringToTrim)
	{
		return Regex.Replace(stringToTrim, "\\s+", " ");
	}

	public object getUriString(object aObjAddr)
	{
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		obj2 = DumpObject(aObjAddr, "_Uri");
		obj3 = DumpString(obj2, "m_String");
		if (obj3 == Globals.NOT_FOUND)
		{
			return DumpString(obj2, "m_AbsoluteUri");
		}
		return obj3;
	}

	public void FindManagedExceptionsForAllThreads()
	{
		int num = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		Globals.HelperFunctions.ResetStatus("Finding .NET Exceptions in all the .NET thread stacks", Globals.g_Debugger.Threads.Count, "Thread");
		for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
			if (Convert.ToString(scriptThreadClass.ClrStackReportNoArgs).GetSafeLength() > 0)
			{
				findManagedExceptions(scriptThreadClass.ThreadID);
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		if (Convert.ToInt32(Globals.g_ThreadExceptionList.Count) > 0)
		{
			displayManagedExceptions();
		}
	}

	public void PopulateDSOCache()
	{
		int num = 0;
		for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
		{
			PopulateDSOCacheForThread(num);
		}
	}

	public void PopulateDSOCacheForThread(int intThreadId)
	{
		NetDbgThread val = Globals.g_Debugger.Threads[intThreadId];
		StringBuilder stringBuilder = new StringBuilder("OS Thread Id: 0x93c (126)\r\nRSP/REG          Object           Name\r\n");
		foreach (ClrRoot item in val.EnumerateStackObjectRoots())
		{
			if (!item.IsInterior)
			{
				stringBuilder.AppendLine($"{item.Address:x} {item.Object:x} {item.Type.Name}");
			}
		}
		if (!Globals.g_DumpStackObjects.ContainsKey(val.ThreadID))
		{
			Globals.g_DumpStackObjects.Add(val.ThreadID, stringBuilder.ToString());
		}
	}

	public void findManagedExceptions(int intThreadId)
	{
		string text = null;
		text = FindObjectFromDSO("Exception", intThreadId, false);
		if (Globals.HelperFunctions.IsNullOrEmpty(text))
		{
			return;
		}
		if (Convert.ToBoolean(Globals.g_ThreadExceptionList.ContainsKey(intThreadId)))
		{
			if (Convert.ToString(text).IndexOf(Convert.ToString(Globals.g_ThreadExceptionList[intThreadId])) >= 0)
			{
				Globals.g_ThreadExceptionList[intThreadId] = text;
			}
			else
			{
				Globals.g_ThreadExceptionList[intThreadId] = Convert.ToString(Globals.g_ThreadExceptionList[intThreadId]) + "," + Convert.ToString(text);
			}
		}
		else
		{
			Globals.g_ThreadExceptionList.Add(intThreadId, text);
		}
	}

	public void displayManagedExceptions()
	{
		int num = 0;
		int num2 = 0;
		string[] array = null;
		int num3 = 0;
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = "";
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("ManagedExceptionsInStacksReport", (SectionType)0);
		val.Title = "Previous .NET Exceptions Report (Exceptions in all .NET Thread Stacks)";
		val.Collapsible = true;
		val.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText><th nowrap>Thread ID</th><th>Exception Type&nbsp;&nbsp;&nbsp;</th><th>Message</th><th nowrap>Stack Trace</th><th>Remote Stack Trace</th>");
		for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
		{
			num2 = Globals.g_ThreadInfoCache.Item(num).ThreadID;
			if (!Convert.ToBoolean(Globals.g_ThreadExceptionList.ContainsKey(num2)))
			{
				continue;
			}
			array = Strings.Split(Convert.ToString((object)Globals.g_ThreadExceptionList[num2]), ",");
			for (num3 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num3 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num3++)
			{
				text = DumpString(array[num3], "_className");
				if (text == Globals.NOT_FOUND)
				{
					text = GetManagedExceptionType(array[num3], bInnerException: false);
				}
				if (!(text == Globals.NOT_FOUND))
				{
					text2 = GetManagedExceptionMsg(array[num3], bInnerException: false);
					text4 = GetStackTraceFromException(array[num3]);
					text4 = Convert.ToString(Regex.Replace(text4, "\\n", "<br>&nbsp;&nbsp;&nbsp;"));
					text3 = DumpString(array[num3], "_remoteStackTraceString");
					text3 = Regex.Replace(text3, "\\n", "<br>&nbsp;&nbsp;&nbsp;");
					CheckForWellKnownExceptionTypes(text, text2, text4, text3, null);
					_ = array[num3];
					val.Write("<tr><td>" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(num)) + "</td>");
					val.Write("<td><b>" + Convert.ToString(text) + "&nbsp;&nbsp;&nbsp;</b></td>");
					val.Write("<td>" + HTMLEncode(text2) + "</td>");
					val.Write("<td nowrap>&nbsp;&nbsp;&nbsp;" + text4 + "</td>");
					if (text3 == Globals.NOT_FOUND)
					{
						val.Write("<td> Remote Stack Trace not available </td>");
					}
					else
					{
						val.Write("<td nowrap>&nbsp;&nbsp;&nbsp;" + Convert.ToString(text3) + "</td>");
					}
					val.Write("</tr>");
				}
			}
		}
		val.Write("</table>");
		if (Convert.ToInt32(Globals.g_ThreadExceptionList.Count) <= 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("The following threads in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> have evidence of <b><u>previous .net exceptions</u></b> on the stack <br><br>(");
		foreach (int key in Globals.g_ThreadExceptionList.Keys)
		{
			stringBuilder.Append(" " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(key)));
		}
		stringBuilder.Append(" )<br><br>");
		Globals.Manager.ReportWarning(stringBuilder.ToString(), "Check the <a href='#ManagedExceptionsInStacksReport" + Convert.ToString(Globals.g_UniqueReference) + "'>Previous .NET Exceptions Report (Exceptions in all .NET Thread Stacks)</a> to view more details of the associated exception", 0, "{35487081-1f33-49d9-9733-706914ad8a3f}");
	}

	public string GetStackTraceFromException(string exceptionObject)
	{
		ClrException exceptionObject2 = Globals.g_Debugger.ClrHeap.GetExceptionObject((ulong)Globals.HelperFunctions.FromHex(exceptionObject));
		if (exceptionObject2 != null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ClrStackFrame item in exceptionObject2.StackTrace)
			{
				stringBuilder.AppendLine(item.DisplayString);
			}
			return stringBuilder.ToString();
		}
		return "";
	}

	private string HTMLEncodeLargeString(string sVal)
	{
		StringBuilder stringBuilder = new StringBuilder(sVal.Length);
		for (int i = 0; i < sVal.Length; i++)
		{
			string value = sVal[i].ToString();
			if (!Regex.IsMatch(sVal[i].ToString(), "[ a-zA-Z0-9]") && (short)sVal[i] != 10)
			{
				value = "&#" + Convert.ToString((short)sVal[i]) + ";";
			}
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public string HTMLEncode(object sVal)
	{
		string text = "";
		string text2 = "";
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (Information.TypeName(sVal) == "String" && !Globals.HelperFunctions.IsNullOrEmpty(sVal) && Convert.ToString(sVal) != "")
		{
			num3 = 10000;
			if (Convert.ToString(sVal).GetSafeLength() > num3)
			{
				return HTMLEncodeLargeString(Convert.ToString(sVal));
			}
			int safeLength = Convert.ToString(sVal).GetSafeLength();
			num4 = safeLength / num3;
			if (safeLength % num3 == 0)
			{
				num4--;
			}
			string[] array = new string[num4 + 1];
			string[] array2 = new string[num4 + 1];
			for (num2 = 0; num2 <= num4; num2++)
			{
				array[num2] = Convert.ToString(sVal).Substring(num2 * num3, (safeLength < num3) ? safeLength : num3);
			}
			new Regex("[ a-zA-Z0-9]", RegexOptions.None);
			for (num2 = 0; num2 <= num4; num2++)
			{
				array2[num2] = "";
				for (num = 1; num <= array[num2].GetSafeLength(); num++)
				{
					text2 = array[Convert.ToInt32(num2)].Substring(num - 1, 1);
					if (!Regex.IsMatch(text2, "[ a-zA-Z0-9]") && (short)text2[0] != 10)
					{
						text2 = "&#" + Convert.ToString((short)text2[0]) + ";";
					}
					array2[num2] += text2;
				}
			}
			for (num2 = 0; num2 <= num4; num2++)
			{
				text += array2[num2];
			}
		}
		return text;
	}

	public bool IsLowerRuntimeVersion(object versionNumber)
	{
		bool result = false;
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		object obj4 = null;
		if (Globals.g_clrModule == null)
		{
			return result;
		}
		Globals.g_clrModule.GetProductVersion(ref obj, ref obj2, ref obj3, ref obj4);
		if ((int)obj4 < (int)versionNumber)
		{
			return true;
		}
		return false;
	}

	public void FindCLRDLL()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (Globals.g_Debugger.ClrVersionInfo == null)
		{
			return;
		}
		IDbgModule val = null;
		if (Globals.g_Debugger.ClrVersionInfo.Version.Major >= 4)
		{
			val = Globals.g_Debugger.GetModuleByModuleName("clr");
		}
		else if (Globals.g_Debugger.ClrVersionInfo.Version.Major == 2)
		{
			val = Globals.g_Debugger.GetModuleByModuleName("mscorwks");
		}
		else if (Globals.g_Debugger.ClrVersionInfo.Version.Major == 1)
		{
			val = Globals.g_Debugger.GetModuleByModuleName("mscorwks");
			if (val == null)
			{
				val = Globals.g_Debugger.GetModuleByModuleName("mscorsvr");
			}
		}
		if (val == null)
		{
			val = Globals.g_Debugger.GetModuleByModuleName("coreclr");
		}
		Globals.g_clrModule = val;
	}

	public void LoadCLRInformation(bool ignoreMiniDumpFailure = false)
	{
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		FindCLRDLL();
		if (Globals.g_clrModule != null && Globals.g_clrModule.ModuleName == "CORECLR")
		{
			Globals.Manager.ReportWarning("Silverlight is loaded in this process so Managed Exception and Managed Hang Analysis was not done", "Open the dump file in a debugger on a machine running the same version of silverlight as the dump file.  Run the command \".loadby sos coreclr\" to load sos for Silverlight analysis.", 0, "{8d193e18-2088-458b-b573-d660269c96c1}");
			return;
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("CLRInformation", (SectionType)0);
		val.Title = "CLR Information";
		val.IncludeInTOC = false;
		val.Write("<a name='CLRINFO" + Convert.ToString(Globals.g_UniqueReference) + "'></a>");
		if (Globals.g_Debugger.ClrVersionInfo != null)
		{
			ClrRuntime clrRuntime = Globals.g_Debugger.ClrRuntime;
			object obj;
			if (clrRuntime == null)
			{
				obj = null;
			}
			else
			{
				DataTarget dataTarget = clrRuntime.DataTarget;
				obj = ((dataTarget != null) ? dataTarget.ClrVersions : null);
			}
			IList<ClrInfo> list = (IList<ClrInfo>)obj;
			bool flag = list != null && list.Count > 1;
			if (Globals.g_Debugger.ClrRuntimes != null && flag)
			{
				val.WriteLine("There are multiple CLR Runtimes loaded in this process:<br>");
			}
			VersionInfo version;
			if (list != null)
			{
				foreach (ClrInfo item in list)
				{
					version = item.Version;
					string text = string.Empty;
					if (flag && AreVersionsEqual(version, Globals.g_Debugger.ClrVersionInfo.Version))
					{
						text = " <i><font color='darkblue'> (this runtime was selected for analysis)</font></i>";
					}
					val.Write($" CLR version = <b>{version.Major}.{version.Minor}.{version.Revision}.{version.Patch}</b>{text}<br>");
				}
			}
			version = Globals.g_Debugger.ClrVersionInfo.Version;
			if (Globals.g_Debugger.ClrRuntime != null)
			{
				val.Write(string.Concat(" Microsoft.Diagnostics.Runtime version = <b>", ((object)Globals.g_Debugger.ClrRuntime).GetType().Assembly.GetName().Version, "</b><br>"));
			}
			if (Convert.ToInt32(version.Major) == 1)
			{
				if (Convert.ToInt32(version.Minor) == 1 && Convert.ToInt32(version.Patch) < 2032)
				{
					Globals.Manager.ReportWarning("The <a href='#DotNetReport" + Convert.ToString(Globals.g_UniqueReference) + "'>CLR version</a> of the .net framework in the dump file is lower than .net framework 1.1 service pack 1", "Please apply <a href='http://support.microsoft.com/kb/885055'>.NET Framework 1.1 Service Pack 1</a> to update the .net runtime", 0, "{6a0b77b0-e51c-431a-a096-3e367d288d4a}");
				}
			}
			else if (Convert.ToInt32(version.Major) >= 2 && Convert.ToInt32(version.Major) < 4 && Convert.ToInt32(version.Patch) < 3053)
			{
				Globals.Manager.ReportWarning("The <a href='#DotNetReport" + Convert.ToString(Globals.g_UniqueReference) + "'>CLR version</a> of the .net framework in the dump file is lower than .net framework 2.0 service pack 2", "Please apply <a href='http://www.microsoft.com/downloads/details.aspx?FamilyID=5b2c0358-915b-4eb5-9b1d-10e506da9d0f&displaylang=en'>Microsoft .NET Framework 2.0 Service Pack 2</a> to update the .net runtime", 0, "{c36800c6-5698-4410-8d6d-dfc0f843c7b1}");
			}
		}
		CheckClrExtension(ignoreMiniDumpFailure);
	}

	private bool AreVersionsEqual(VersionInfo ver1, VersionInfo ver2)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (ver1.Major == ver2.Major && ver1.Minor == ver2.Minor && ver1.Revision == ver2.Revision)
		{
			return ver1.Patch == ver2.Patch;
		}
		return false;
	}

	public void GetClrVersion(ref int Major, ref int Minor)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (Globals.g_Debugger.ClrVersionInfo != null)
		{
			Major = Globals.g_Debugger.ClrVersionInfo.Version.Major;
			Minor = Globals.g_Debugger.ClrVersionInfo.Version.Minor;
		}
	}

	public bool IsClrExtensionExecuting()
	{
		return Globals.g_CLRExtensionExecuting;
	}

	public bool IsClrLoaded()
	{
		int Major = 0;
		int Minor = 0;
		GetClrVersion(ref Major, ref Minor);
		return Major > 0;
	}

	public bool IsClrV2()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return Globals.g_Debugger.ClrVersionInfo.Version.Major == 2;
	}

	public CacheFunctions.ScriptModuleClass GetClrModule()
	{
		return Globals.g_ModuleCache.GetModuleByName(Globals.g_clrModule.ModuleName);
	}

	public void CheckClrExtension(bool ignoreMiniDumpFailure = false)
	{
		ClrRuntime clrRuntime = Globals.g_Debugger.ClrRuntime;
		Globals.g_CLRExtensionExecuting = clrRuntime != null;
		if (clrRuntime == null)
		{
			return;
		}
		if (Globals.g_Debugger.DumpType == "MINIDUMP")
		{
			if (!ignoreMiniDumpFailure)
			{
				Globals.Manager.ReportOther("The .NET runtime was loaded in the process in <b>" + Globals.g_Debugger.DumpFileShortName + "</b>. The dump file is a minidump.  Not all CLR information is present in a minidump, therefore Managed analysis might be incomplete.", "If deeper .NET analysis is required, collect a full dump file", "Notification", "notificationicon.png", 0, "{54d75584-7aa2-4193-8771-0829d3d2c8dc}");
			}
			return;
		}
		ClrHeap clrHeap = Globals.g_Debugger.ClrHeap;
		if (clrHeap == null || !clrHeap.CanWalkHeap)
		{
			Globals.Manager.ReportOther("The .NET runtime was loaded in the process, but the managed heap is in an invalid state. Managed analysis might be incomplete.", "", "Notification", "notificationicon.png", 0, "{54d75584-7aa2-4193-8771-0829d3d2c8db}");
		}
	}

	public void AnalyzeWCFServiceHost()
	{
		if (!Convert.ToBoolean(Globals.g_IsWCFServiceHost))
		{
			return;
		}
		string setting = ConfigHelper.GetSetting("maxWcfServiceConfigs");
		string setting2 = ConfigHelper.GetSetting("showWcfServiceDetails");
		int result = 25;
		bool result2 = true;
		if (!string.IsNullOrEmpty(setting))
		{
			int.TryParse(setting, out result);
		}
		if (!string.IsNullOrEmpty(setting2))
		{
			bool.TryParse(setting2, out result2);
		}
		AnalyzeManagedWCF analyzeManagedWCF = new AnalyzeManagedWCF(Globals.g_Debugger);
		List<WCFServiceSummary> wCFSummary = analyzeManagedWCF.GetWCFSummary(result2, result);
		if (wCFSummary.Count <= 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing WCF Service");
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("WCFTHROTTLE", (SectionType)0);
		val.Title = "WCF Service Report";
		val.Collapsible = true;
		string text = analyzeManagedWCF.GenerateHTMLReportWCFSummary(wCFSummary);
		val.WriteLine(text);
		if (analyzeManagedWCF.DisplayWcfServiceHostLimitReached)
		{
			val.WriteLine($"<br>Details were only displayed for the first {result} out of {wCFSummary.Count} WCF ServiceHosts. To view more details, modify the <b>maxWcfServiceConfigs</b> setting in the DebugDiag.AnalysisRules.dll.config file and rerun the analysis");
		}
		if (result2)
		{
			ReportSection val2 = Globals.Manager.CurrentSection.AddChildSection("WCFCONFIGURATION", (SectionType)0);
			val2.Title = "WCF Service Configuration Report";
			val2.Collapsible = true;
			foreach (WCFServiceSummary item in wCFSummary)
			{
				val2.WriteLine($"<h2 id={item.ServiceType + Globals.g_UniqueReference}><b>Service: {item.ServiceType} </b><br></h2>");
				val2.WriteLine("<div style='padding-left:20px'>");
				string text2 = analyzeManagedWCF.GenerateHTMLReportWCFConfiguration(item.ServiceConfiguration);
				string text3 = analyzeManagedWCF.GenerateHTMLReportWCFChannel(item.ServiceConfiguration.Channels);
				val2.Write("<h2><b>Configuration/Behaviors</b></h2>");
				val2.Write(text2);
				val2.Write("<br>");
				val2.Write(text3);
				val2.WriteLine("</div>");
				val2.Write("<br><br>");
			}
		}
		foreach (DebugDiagReportIssue item2 in analyzeManagedWCF.CheckForThrottlingIssues(wCFSummary))
		{
			string description = item2.Description;
			string recommendation = item2.Recommendation;
			int weight = item2.Weight;
			description = description.Replace("#DUMP_FILE#", Globals.g_ShortDumpFileName);
			recommendation = recommendation.Replace("#UR#", Globals.g_UniqueReference);
			if (item2.Type.Equals("Error"))
			{
				Globals.Manager.ReportError(description, recommendation, weight, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
			}
			else if (item2.Type.Equals("Warning"))
			{
				Globals.Manager.ReportWarning(description, recommendation, weight, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
			}
		}
	}

	public void GetFlowThrottleValues(object serviceThrottle, string flowThrottlePropName, ref int count, ref int capacity, ref double pct)
	{
		object obj = null;
		obj = DumpObject(serviceThrottle, flowThrottlePropName);
		count = Convert.ToInt32(DumpLong(obj, "count"));
		capacity = Convert.ToInt32(DumpLong(obj, "capacity"));
		pct = count / capacity;
	}

	public string GetTypeNameFromSystemType(object objectAddress)
	{
		ClrObject objectAt = GetObjectAt(objectAddress);
		if (objectAt == null || objectAt.IsNull())
		{
			return Globals.NOT_FOUND;
		}
		ClrType val = objectAt.GetHeapType();
		if (val.IsRuntimeType)
		{
			val = val.GetRuntimeType(objectAt.GetValue());
			if (val == null)
			{
				return Globals.NOT_FOUND;
			}
		}
		return val.Name;
	}

	private ClrObject GetObjectAt(object objectAddress)
	{
		if (Globals.g_Debugger.ClrHeap == null)
		{
			return null;
		}
		ulong num = ((!(objectAddress is ulong)) ? ((ulong)Globals.HelperFunctions.FromHex((string)objectAddress)) : ((ulong)objectAddress));
		return (ClrObject)(dynamic)ClrMemDiagExtensions.GetDynamicObject(Globals.g_Debugger.ClrHeap, num);
	}

	private object DumpObjectInternalClr(object objectAddress, string fieldName)
	{
		if (IsBadPtr(objectAddress))
		{
			return Globals.NOT_FOUND;
		}
		ClrObject objectAt = GetObjectAt(objectAddress);
		if (objectAt == null || objectAt.IsNull())
		{
			return Globals.NOT_FOUND;
		}
		return GetClrObjectField(objectAt, fieldName);
	}

	private object GetClrObjectField(ClrObject obj, string fieldName)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		try
		{
			ClrType heapType = obj.GetHeapType();
			ClrInstanceField fieldByName = heapType.GetFieldByName(fieldName);
			if (fieldByName == null)
			{
				return null;
			}
			if (((ClrField)fieldByName).IsValueClass())
			{
				ulong fieldAddress = fieldByName.GetFieldAddress(obj.GetValue(), false);
				return (object)new ClrObject(heapType.Heap, ((ClrField)fieldByName).Type, fieldAddress, true);
			}
			return fieldByName.GetFieldValue(obj.GetValue());
		}
		catch (Exception)
		{
			return Globals.NOT_FOUND;
		}
	}

	public string DumpObject(object objectAddress, string fieldName)
	{
		object obj = DumpObjectInternalClr(objectAddress, fieldName);
		if (IsBadPtr(obj))
		{
			return Globals.NOT_FOUND;
		}
		return $"{obj:x}";
	}

	private bool IsBadPtr(object objectAddress)
	{
		try
		{
			if (objectAddress == null)
			{
				return true;
			}
			if (objectAddress is string)
			{
				if (objectAddress == Globals.NOT_FOUND)
				{
					return true;
				}
				if (Convert.ToUInt64((string)objectAddress, 16) == 0L)
				{
					return true;
				}
			}
			if (objectAddress is ulong && (ulong)objectAddress == 0L)
			{
				return true;
			}
		}
		catch (Exception)
		{
			return true;
		}
		return false;
	}

	public string DumpString(object objectAddress, string fieldName)
	{
		object obj = DumpObjectInternalClr(objectAddress, fieldName);
		if (obj == null)
		{
			return Globals.NOT_FOUND;
		}
		return (string)obj;
	}

	public object DumpStringVal(object stringAddress)
	{
		throw new NotImplementedException("no longer used");
	}

	public int DumpShort(object objectAddress, string fieldName)
	{
		object obj = DumpObjectInternalClr(objectAddress, fieldName);
		if (obj is string && obj == Globals.NOT_FOUND)
		{
			return -1;
		}
		return Convert.ToInt32(obj);
	}

	public object DumpByte(object objectAddress, string fieldName)
	{
		dynamic val = DumpObjectInternalClr(objectAddress, fieldName);
		if (val is string && val == Globals.NOT_FOUND)
		{
			return Globals.NOT_FOUND;
		}
		return Convert.ToByte(val);
	}

	public object DumpLong(object objectAddress, string fieldName)
	{
		dynamic val = DumpObjectInternalClr(objectAddress, fieldName);
		if (val is string && val == Globals.NOT_FOUND)
		{
			return Globals.NOT_FOUND;
		}
		return Convert.ToInt32(val);
	}

	public object DumpQuad(object objectAddress, string fieldName)
	{
		dynamic val = DumpObjectInternalClr(objectAddress, fieldName);
		if (val is string && val == Globals.NOT_FOUND)
		{
			return Globals.NOT_FOUND;
		}
		return Convert.ToInt64(val).ToString("x16");
	}

	public string DumpDateTimeAsString(object dateTimeAddress)
	{
		dynamic objectAt = GetObjectAt(dateTimeAddress);
		if (objectAt == null)
		{
			return null;
		}
		return DateTime.FromBinary((long)(ulong)objectAt.dateData).ToString("MM/dd/yyyy HH:mm:ss");
	}

	public object[] DumpObjectArrayRaw(object objectString)
	{
		if (IsBadPtr(objectString))
		{
			return null;
		}
		ClrObject objectAt = GetObjectAt(objectString);
		if (objectAt == null || objectAt.IsNull())
		{
			return null;
		}
		dynamic val = objectAt;
		string[] array = new string[objectAt.GetLength()];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = val[i].GetValue().ToString("x");
		}
		return array;
	}

	public object[] DumpObjectArray(object objectAddress, string fieldName)
	{
		dynamic val = DumpObjectInternalClr(objectAddress, fieldName);
		return DumpObjectArrayRaw(val);
	}

	public void AnalyzeWCFRequest()
	{
		if (Convert.ToBoolean(Globals.g_IsWCFServiceHost))
		{
			string setting = ConfigHelper.GetSetting("maxWcfRequests");
			int result = 100;
			if (!string.IsNullOrEmpty(setting))
			{
				int.TryParse(setting, out result);
			}
			AnalyzeManagedWCF analyzeManagedWCF = new AnalyzeManagedWCF(Globals.g_Debugger);
			List<WCFRequestItem> wCFRequest = analyzeManagedWCF.GetWCFRequest(result);
			wCFRequest = (from o in wCFRequest
				orderby o.CallDuration descending, o.RequestState descending
				select o).ToList();
			if (wCFRequest.Count > 0)
			{
				ReportSection val = Globals.Manager.CurrentSection.AddChildSection("WCFREQUEST", (SectionType)0);
				val.Title = "WCF Request Report";
				val.Collapsible = true;
				string text = analyzeManagedWCF.GenerateHTMLReportWCFRequest(wCFRequest);
				val.WriteLine(text);
				if (analyzeManagedWCF.DisplayWcfRequestLimitReached)
				{
					val.WriteLine($"<br>Details were only displayed for the first {result} out of {wCFRequest.Count} WCF Requests. To view more details, modify the <b>maxWcfRequests</b> setting in the DebugDiag.AnalysisRules.dll.config file and rerun the analysis");
				}
				foreach (DebugDiagReportIssue item in analyzeManagedWCF.CheckForExceptionInRequest(wCFRequest))
				{
					string description = item.Description;
					string recommendation = item.Recommendation;
					description = description.Replace("#UR#", Globals.g_UniqueReference);
					recommendation = recommendation.Replace("#UR#", Globals.g_UniqueReference);
					if (item.Type.Equals("Error"))
					{
						Globals.Manager.ReportError(description, recommendation, 0, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
					}
					else if (item.Type.Equals("Warning"))
					{
						Globals.Manager.ReportWarning(description, recommendation, 0, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
					}
				}
			}
		}
		if (!Convert.ToBoolean(Globals.g_IsWCFClient))
		{
			return;
		}
		AnalyzeManagedWCF analyzeManagedWCF2 = new AnalyzeManagedWCF(Globals.g_Debugger);
		List<AnalyzeManagedWCF.WcfClientRequest> wcfClientRequest = analyzeManagedWCF2.GetWcfClientRequest();
		List<WcfClientConnectionSummary> wcfClientConnectionSumary = analyzeManagedWCF2.GetWcfClientConnectionSumary(wcfClientRequest);
		if (wcfClientRequest.Count <= 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing WCF Service");
		ReportSection val2 = Globals.Manager.CurrentSection.AddChildSection("WCFCLIENTREQUEST", (SectionType)0);
		val2.Title = "WCF Client Report";
		val2.Collapsible = true;
		val2.Write("<h2 id='WCFCLIENTCONN{0}'><b>WCF Connection Summary</b></h2>", new object[1] { Globals.g_UniqueReference });
		val2.WriteLine("<div style='padding-left:20px'>");
		string text2 = analyzeManagedWCF2.GenerateHTMLReportWCFClientSummary(wcfClientConnectionSumary);
		val2.WriteLine(text2);
		val2.WriteLine("</div>");
		val2.WriteLine("<br>");
		wcfClientRequest.ForEach(delegate(AnalyzeManagedWCF.WcfClientRequest i)
		{
			i.ThreadID = (i.ThreadID.Equals("---") ? i.ThreadID : Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(i.ThreadID)));
		});
		wcfClientRequest = wcfClientRequest.OrderByDescending((AnalyzeManagedWCF.WcfClientRequest o) => o.Endpoint).ToList();
		val2.Write("<h2><b>WCF Client Request</b></h2>");
		val2.WriteLine("<div style='padding-left:20px'>");
		string text3 = analyzeManagedWCF2.GenerateHTMLReportWCFClientRequest(wcfClientRequest);
		val2.WriteLine(text3);
		val2.WriteLine("</div>");
		foreach (DebugDiagReportIssue item2 in analyzeManagedWCF2.CheckForWcfClientLimits(wcfClientConnectionSumary))
		{
			string description2 = item2.Description;
			string recommendation2 = item2.Recommendation;
			int weight = item2.Weight;
			description2 = description2.Replace("#DUMP_FILE#", Globals.g_ShortDumpFileName);
			description2 = description2.Replace("#UR#", Globals.g_UniqueReference);
			recommendation2 = recommendation2.Replace("#UR#", Globals.g_UniqueReference);
			if (item2.Type.Equals("Error"))
			{
				Globals.Manager.ReportError(description2, recommendation2, weight, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
			}
			else if (item2.Type.Equals("Warning"))
			{
				Globals.Manager.ReportWarning(description2, recommendation2, weight, "{6f31c949-ee03-4a9e-8c19-d34a79859186}");
			}
		}
	}

	public void GetWCFThreadsInformation()
	{
		int num = 0;
		string text = "";
		string[] array = null;
		string text2 = "";
		int num2 = 0;
		string text3 = "";
		string[] array2 = null;
		string text4 = "";
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		object obj4 = null;
		object obj5 = null;
		object obj6 = null;
		object obj7 = null;
		object obj8 = null;
		if (Globals.g_DumpStackObjects.Count == 0)
		{
			PopulateDSOCache();
		}
		if (!Convert.ToBoolean(Globals.g_IsWCFServiceHost))
		{
			return;
		}
		for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
		{
			string clrStackReportNoArgs = Globals.g_ThreadInfoCache.Item(num).ClrStackReportNoArgs;
			text = "System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke";
			if (Convert.ToString((object)clrStackReportNoArgs).IndexOf(text) < 0)
			{
				continue;
			}
			array = Strings.Split(Convert.ToString((object)Globals.g_DumpStackObjects[num]), "\n");
			text2 = "System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem";
			for (num2 = 0; num2 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num2++)
			{
				if (array[num2].IndexOf(text2) < 0)
				{
					continue;
				}
				text3 = array[num2];
				text3 = Convert.ToString(normalizeWhitespace(text3));
				array2 = Strings.Split(text3);
				if (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1) < 2)
				{
					break;
				}
				text4 = array2[1];
				obj6 = DumpObject(text4, "state");
				obj7 = getManagedObjectType(obj6);
				obj3 = Globals.NOT_FOUND;
				if (Convert.ToString(obj7).IndexOf("ReceiveRequestAsyncResult") >= 0)
				{
					obj = obj6;
					if (obj != Globals.NOT_FOUND)
					{
						obj2 = DumpObject(obj, "innerRequestContext");
						if (obj2 != Globals.NOT_FOUND)
						{
							obj3 = DumpObject(obj2, "result");
						}
					}
				}
				else if (Convert.ToString(obj7).IndexOf("HostedHttpRequestAsyncResult") >= 0)
				{
					obj3 = obj6;
				}
				else if (Convert.ToString(obj7).IndexOf("TracingAsyncCallbackState") >= 0)
				{
					obj8 = obj6;
					if (obj8 != Globals.NOT_FOUND)
					{
						obj3 = DumpObject(obj8, "innerState");
					}
				}
				if (obj3 != Globals.NOT_FOUND)
				{
					obj4 = DumpObject(obj3, "context");
					if (obj4 != Globals.NOT_FOUND)
					{
						obj5 = DumpObject(obj4, "_context");
						Globals.g_WCFIOSchedulerThreads.Add(Globals.HelperFunctions.FromHex((string)obj5), num);
					}
				}
				break;
			}
		}
	}

	public AnalyzedThreadClass getDotNetAnalysis(CacheFunctions.ScriptThreadClass Thread, object FrameID)
	{
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1902: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e68: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		string text = "";
		double num = 0.0;
		int num2 = 0;
		double num3 = 0.0;
		object obj = null;
		bool flag = false;
		object obj2 = null;
		object obj3 = null;
		string[] array = null;
		int num4 = 0;
		bool flag2 = false;
		object obj4 = null;
		AnalyzedThreadClass analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		string clrStackReportNoArgs = Thread.ClrStackReportNoArgs;
		AnalyzedThreadClass analyzedThreadClass2 = null;
		if ((Thread.FindFrameInStack("WAITUNTILGCCOMPLETE") > -1 || Thread.FindFrameInStack("WAIT_FOR_GC_DONE") > -1) && Globals.g_GCThread != Thread.ThreadID)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGONGC";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting for .net garbage collection to finish.";
			if (Convert.ToInt32(Globals.g_GCThread) != -1)
			{
				analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + " Thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(Globals.g_GCThread))) + " triggered the garbage collection.";
				analyzedThreadClass.Recommendation = "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details";
			}
			else
			{
				analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + "The current set of scripts were not able to determine which thread induced GC";
				analyzedThreadClass.Recommendation = "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details";
			}
			if (Convert.ToString(Globals.g_PreemptiveGCDisabledThreads) != "")
			{
				analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + "The gargage collector thread wont start doing its work till the time the threads which have pre-emptive GC disabled have finished executing. The following threads have pre-emptive GC disabled " + Convert.ToString(Globals.g_PreemptiveGCDisabledThreads);
				analyzedThreadClass.Recommendation = "Review the callstacks for the threads that have preemptive GC Disabled to see what they are doing which is preventing the GC to run. Review the blog <a href='http://blogs.msdn.com/tess/archive/2007/03/12/net-hang-case-study-the-gc-loader-lock-deadlock-a-story-of-mixed-mode-dlls.aspx'>.NET Hang Case Study: The GC-Loader Lock Deadlock (a story of mixed mode dlls)</a> that gives some information on how to debug these issue more";
			}
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("GARBAGECOLLECTGENERATION") > -1 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.Caching.CacheCommon.GcCollect") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETGCTHREADINDUCEDGCDUETOCACHE";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "calling the .Net Garbage Collector but they got invoked as a result of <b>ASP.NET Cache Scavenging</b>";
			analyzedThreadClass.Recommendation = "Please refer to these articles to understand why ASP.NET calls GC.Collect to reduce the memory footprint of large caches as available memory becomes scarce:<br/><ol><li><a href='http://www.aspx.net/web-forms/tutorials/moving-to-aspnet-20/caching'>ASP.NET Caching</a></li><li><a href='http://blogs.msdn.com/b/praveeny/archive/2006/12/11/asp-net-2-0-cache-objects-get-trimmed-when-you-have-low-available-memory.aspx'>ASP.NET 2.0 Cache Objects get trimmed when you have low Available Memory</a></li><ol>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("GARBAGECOLLECTGENERATION") > -1 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.GC.Collect") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETGCTHREADINDUCEDGC";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "calling <b>System.GC.Collect</b> to force .Net Garbage Collection";
			analyzedThreadClass.Recommendation = "The CLR garbage collector is self-tuning and you should refrain from calling <b>GC.Collect</b> explicitly unless you are very sure that is required for improving the memory for your application. Please refer to these articles for more information<br/><ol><li><a href='http://blogs.msdn.com/b/ricom/archive/2004/11/29/271829.aspx'>When to call GC.Collect()</a></li><li><a href='http://blogs.msdn.com/b/scottholden/archive/2004/12/28/339733.aspx'>The perils of GC.Collect (or when to use GC.Collect)</a></li><ol>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Xml.Xsl.XslTransform.Compile") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETTHREADINGDOINGXSLTCOMPILATION";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "performing <b>XSLT Compilation</b>";
			analyzedThreadClass.Recommendation = "Please check if you are running in to the issue described in <a href='http://blogs.msdn.com/b/tess/archive/2010/05/05/net-memory-leak-xslcompiledtransform-and-leaked-dynamic-assemblies.aspx'>.NET Memory Leak: XslCompiledTransform and leaked dynamic assemblies </a> ";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("JIT_MONTRYENTER") > -1)
		{
			analyzedThreadClass.Category = "DOTNETSPINNINGTOENTERLOCK";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "spinning waiting to enter a .net lock";
			analyzedThreadClass.Recommendation = "";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInClrStack("SYSTEM.WEB.HTTPAPPLICATIONSTATELOCK.ACQUIREREAD") > -1 || Thread.FindFrameInClrStack("SYSTEM.WEB.HTTPAPPLICATIONSTATELOCK.ACQUIREWRITE") > -1)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGTOACQUIREHTTPAPPLICATIONLOCK";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting to acquire a read\\write lock on the <b>HttpApplicationState </b>";
			analyzedThreadClass.Recommendation = "";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			text = "-1";
			if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				text = GetOwnerIdForHttpApplicationStateLock(Thread);
			}
			if (text != "-1")
			{
				analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + " which thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLinkFromDecSystemID(Convert.ToInt32(text))) + " is currently holding";
				analyzedThreadClass.Recommendation = " Look at the callstack of thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLinkFromDecSystemID(Convert.ToInt32(text))) + " to see what the thread is waiting on and why it is not releasing the lock";
				analyzedThreadClass.Category = Convert.ToString(analyzedThreadClass.Category) + Convert.ToString(text);
			}
			else if (!Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock because the managed debugger extension commands failed to execute";
			}
			else
			{
				analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock";
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("SYNCBLOCK::WAIT") > -1 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Threading.Monitor.Wait") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETTHREADINGMONITORWAIT";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting in <b>System.Threading.Monitor.Wait</b>";
			analyzedThreadClass.Recommendation = "Threads waiting in <a href='http://msdn.microsoft.com/en-us/library/syehfawa.aspx'>Monitor.Wait </a> are actually waiting to re-acquire a lock which they released. The signal to reacquire the lock will be given by a call to <b>Monitor.Pulse</b> or <b>Monitor.PulseAll</b> or when the timeout is hit. <br/><br/> Look at the callstack of the thread to see which function is making a call to Monitor.Wait and then review code to find out the function which is supposed to call <b>Monitor.Pulse</b> or <b>Monitor.PulseAll</b> and see why that function is not getting called.";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("JITUTIL_MONCONTENTION") > -1 || Thread.FindFrameInClrStack("AWARELOCK::ENTER") > -1 || Thread.FindFrameInClrStack("JITUTIL_MONRELIABLECONTENTION") > -1 || Thread.FindFrameInStack("JIT_MONENTERWORKER_PORTABLE") > -1 || Thread.FindFrameInClrStack("SYSTEM.THREADING.MONITOR.ENTER") > -1)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGTOENTERLOCK";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting to enter a .NET Lock";
			text = "";
			if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				text = IsWaitingOnSyncblk(Thread.ThreadID);
			}
			analyzedThreadClass.KeyPartOne = text;
			if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.Compilation.BuildManager") >= 0)
			{
				if (text != "")
				{
					analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + " while performing compilation which is currently held by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text)));
					analyzedThreadClass.Category = Convert.ToString(analyzedThreadClass.Category) + "COMPILATION" + Convert.ToString(text);
					if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) == 4)
					{
						analyzedThreadClass.Recommendation = " Review the callstack of thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text))) + " to see what it is currently doing.<br> <br> There is an issue in .NET Framework 4.0 which causes performance issues during compilation and the details are in <a href='https://connect.microsoft.com/VisualStudio/feedback/details/582066/asp-net-4-precompilation-and-performance-problems'>asp-net-4-precompilation-and-performance-problems</a>. This issue might surface when the site is marked as non-updatable.";
					}
					else
					{
						analyzedThreadClass.Recommendation = " Review the callstack of thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text))) + " to see what it is currently doing.";
					}
				}
				else
				{
					analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + " while performing compilation";
					if (!Globals.AnalyzeManaged.IsClrExtensionExecuting())
					{
						analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock because the managed debugger extension commands failed to execute";
					}
					else
					{
						analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock";
					}
				}
			}
			else if (text != "")
			{
				analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + " which is owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text)));
				analyzedThreadClass.Recommendation = " Look at the callstack of thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(Convert.ToInt32(text))) + " to see what the thread is waiting on and why it is not releasing the lock.";
			}
			else if (!Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock because the managed debugger extension commands failed to execute";
			}
			else
			{
				analyzedThreadClass.Recommendation = "The current set of debugger scripts were not able to determine which thread is holding the lock";
			}
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (clrStackReportNoArgs.IndexOf("Command.ExecuteReader") >= 0 || clrStackReportNoArgs.IndexOf("Command.ExecuteNonQuery") >= 0 || clrStackReportNoArgs.IndexOf("SPSqlClient.ExecuteQuery") >= 0 || clrStackReportNoArgs.IndexOf("SqlDataReader.Get") >= 0 || clrStackReportNoArgs.IndexOf("Command.ExecuteScalar") >= 0)
		{
			object obj5 = null;
			object obj6 = null;
			object obj7 = null;
			object obj8 = null;
			object obj9 = null;
			object obj10 = null;
			object obj11 = null;
			object obj12 = null;
			int num5 = 0;
			string[] array2 = null;
			int num6 = 0;
			object obj13 = null;
			string text2 = "";
			string text3 = "";
			num5 = 0;
			obj12 = FindObjectFromDSO("System.Data.SqlClient.SqlCommand", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj12))
			{
				obj6 = obj12;
				num5 = 1;
			}
			if (obj6 == null)
			{
				obj12 = FindObjectFromDSO("System.Data.OracleClient.OracleCommand", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj12))
				{
					obj6 = obj12;
					num5 = 2;
				}
			}
			dynamic val = null;
			string text4 = null;
			if (obj6 == null)
			{
				text4 = FindObjectFromDSO("System.Data.SqlClient.SqlDataReader", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(text4))
				{
					val = GetObjectAt(text4);
					if ((!val._command.IsNull()))
					{
						obj6 = ((ClrObject)val._command).GetValue();
						num5 = 1;
					}
				}
			}
			if (obj6 == null)
			{
				text4 = FindObjectFromDSO("System.Data.OracleClient.OracleDataReader", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(text4))
				{
					val = GetObjectAt(text4);
					if ((!val._command.IsNull()))
					{
						obj6 = ((ClrObject)val._command).GetValue();
						num5 = 2;
					}
				}
			}
			obj12 = FindObjectFromDSO("System.Data.OleDb.OleDbCommand", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj12))
			{
				obj6 = obj12;
				num5 = 4;
			}
			switch (num5)
			{
			case 1:
				if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) == 1)
				{
					obj5 = DumpString(obj6, "cmdText");
					obj10 = DumpShort(obj6, "_timeout");
					obj10 = normalizeWhitespace((string)obj10);
					obj7 = DumpObject(obj6, "_activeConnection");
					obj8 = DumpObject(obj7, "_constr");
					obj9 = DumpString(obj8, "_displayString");
					obj11 = DumpShort(obj8, "_connectTimeout");
					break;
				}
				obj5 = DumpString(obj6, "_commandText");
				obj10 = DumpShort(obj6, "_commandTimeout");
				obj10 = normalizeWhitespace(obj10.ToString());
				obj7 = DumpObject(obj6, "_activeConnection");
				obj8 = DumpObject(obj7, "_userConnectionOptions");
				obj9 = DumpString(obj8, "_usersConnectionString");
				obj11 = DumpShort(obj8, "_connectTimeout");
				if (!(Convert.ToString(DumpObject(obj6, "_parameters")) != "NOT_FOUND"))
				{
					break;
				}
				array2 = (string[])DumpObjectArray(DumpObject(DumpObject(obj6, "_parameters"), "_items"), "_items");
				if (Globals.HelperFunctions.IsNullOrEmpty(array2) || array2 == null)
				{
					break;
				}
				for (num6 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array2, 1); num6 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1); num6++)
				{
					if (array2[num6] == "0")
					{
						continue;
					}
					obj13 = DumpString(array2[num6], "_parameterName");
					if (!(Convert.ToString(obj13) != "NOT_FOUND"))
					{
						continue;
					}
					dynamic objectAt = GetObjectAt(array2[num6]);
					if (objectAt._value == null || objectAt._value is ClrNullValue)
					{
						text2 = text2 + Convert.ToString(HTMLEncode("<Not Set>")) + "<br/>";
						continue;
					}
					string text5 = DumpObject(array2[num6], "_value");
					string text6 = Convert.ToString(getManagedObjectType(text5)).ToUpper();
					switch (text6)
					{
					case "SYSTEM.INT32":
					case "SYSTEM.INT64":
						text5 = ((long)objectAt._value).ToString();
						break;
					case "SYSTEM.STRING":
						text5 = objectAt._value;
						break;
					case "SYSTEM.DATETIME":
						text5 = DumpDateTimeAsString(text5);
						break;
					default:
						text5 = $"Object of Type : {text6} at address: {Globals.HelperFunctions.GetAsHexString((ulong)objectAt._value.GetValue())}";
						break;
					}
					text2 = text2 + HTMLEncode(obj13) + " = " + HTMLEncode(text5) + "<br/>";
				}
				break;
			case 2:
			case 4:
				obj5 = DumpString(obj6, "_commandText");
				obj7 = DumpObject(obj6, "_connection");
				obj8 = DumpObject(obj7, "_userConnectionOptions");
				obj9 = DumpString(obj8, "_usersConnectionString");
				break;
			}
			if (text2.GetSafeLength() > 0)
			{
				text3 = "<br/><br/>It is using the following command parameters <br/>" + text2;
			}
			analyzedThreadClass.Category = "DOTNETWAITINGINCOMMANDEXECUTEREADER";
			analyzedThreadClass.Description = "waiting on data to be returned from the database server";
			analyzedThreadClass.Recommendation = "Click the link for each thread to view the database commands being executed. If the database calls are taking longer than expected, the root of the problem may be in the database tier. Ensure that the commands are expected, then review the database server to find root cause.";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass.AdditionalCLRInfo = "The current executing command is : <font color=blue>" + Convert.ToString(obj5) + "</font> and the command timeout is set to <font color=blue><b>" + Convert.ToString(obj10) + " </font>seconds.</b> " + text3 + " <br><br> The connection string for this connection : <font color=blue>" + Convert.ToString(Globals.HelperFunctions.MaskPwd((string)obj9)) + "</font> and the connection timeout : <font color=blue>" + Convert.ToString(obj11) + " </font>seconds. <br><br>";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("Connection.Open") >= 0)
		{
			object obj14 = null;
			object obj15 = null;
			object obj16 = null;
			string text7 = "";
			object obj17 = null;
			int num7 = 0;
			object obj18 = null;
			obj17 = FindObjectFromDSO("System.Data.SqlClient.SqlConnectionString", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj17))
			{
				obj15 = obj17;
				num7 = 0;
			}
			obj17 = FindObjectFromDSO("System.Data.OracleClient.OracleConnectionString", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj17))
			{
				obj15 = obj17;
				num7 = 1;
			}
			obj17 = FindObjectFromDSO("System.Data.OracleClient.OracleConnectionString", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj17))
			{
				obj15 = obj17;
				num7 = 2;
			}
			obj17 = FindObjectFromDSO("System.Data.OleDb.OleDbConnectionString", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj17))
			{
				obj15 = obj17;
				num7 = 3;
			}
			if (Globals.HelperFunctions.IsNullOrEmpty(obj15))
			{
				obj18 = FindObjectFromDSO("System.Data.SqlClient.SqlConnection", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj18))
				{
					num7 = 0;
					obj15 = DumpObject(obj18, "_userConnectionOptions");
				}
				obj18 = FindObjectFromDSO("System.Data.OracleClient.OracleConnection", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj18))
				{
					num7 = 1;
					obj15 = DumpObject(obj18, "_userConnectionOptions");
				}
				obj18 = FindObjectFromDSO("System.Data.Odbc.OdbcConnection", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj18))
				{
					num7 = 2;
				}
				obj18 = FindObjectFromDSO("System.Data.OleDb.OleDbConnection", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj18))
				{
					num7 = 3;
				}
			}
			switch (num7)
			{
			case 0:
				obj14 = DumpString(obj15, "_usersConnectionString");
				obj16 = DumpShort(obj15, "_connectTimeout");
				text7 = " and the connection timeout is set to be " + Convert.ToString(obj16) + "</font> seconds. <br></div><br><br>";
				break;
			case 1:
				obj14 = DumpString(obj15, "_usersConnectionString");
				break;
			case 2:
				obj14 = DumpString(obj15, "_usersConnectionString");
				break;
			case 3:
				obj14 = DumpString(obj15, "_usersConnectionString");
				break;
			}
			if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Data.ProviderBase.DbConnectionPool.GetConnection") >= 0)
			{
				analyzedThreadClass.Category = "DOTNETWAITINGINGETCONNECTIONFROMPOOL";
				analyzedThreadClass.IsWarning = true;
				analyzedThreadClass.Description = Convert.ToString("trying to open a data base connection") + " and waiting to get a connection from the connection pool";
				analyzedThreadClass.Recommendation = "Too may threads in this state would indicate that there are no more connections available in the connection pool either due to a connection leak or due to high load on the application. Review the <a href='#ADODOTNET" + Convert.ToString(Globals.g_UniqueReference) + "'>ADO.NET Connections Report</a> to check the connection pooling information for this connection.";
				analyzedThreadClass.KeyPartOne = "0";
				analyzedThreadClass.KeyPartTwo = "0";
			}
			else
			{
				analyzedThreadClass.Category = "DOTNETWAITINGINCONNECTIONOPEN";
				analyzedThreadClass.IsWarning = true;
				analyzedThreadClass.Description = "trying to open a data base connection";
				analyzedThreadClass.Recommendation = "Too may threads trying to open up a database connection would indicate some problem with the remote server or underlying network. You may want to verify that the TCP Chimney feature is disabled on both the server and the client";
				analyzedThreadClass.KeyPartOne = "0";
				analyzedThreadClass.KeyPartTwo = "0";
			}
			analyzedThreadClass.AdditionalCLRInfo = "The connection String is <font color=blue>" + Convert.ToString(Globals.HelperFunctions.MaskPwd((string)obj14)) + "</font>" + text7;
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Net.HttpWebRequest.GetResponse") >= 0 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Net.HttpWebRequest.GetRequestStream") >= 0)
		{
			object obj19 = null;
			bool flag3 = false;
			object obj20 = null;
			object obj21 = null;
			string text8 = "";
			string text9 = null;
			int num8 = 0;
			int num9 = 0;
			bool flag4 = false;
			object obj22 = null;
			string text10 = "";
			flag3 = false;
			if (Thread.FindFrameInStack("WS2_32!") > -1 || Thread.FindFrameInStack("MSWSOCK!") > -1)
			{
				flag3 = true;
				analyzedThreadClass.Category = "DOTNETWEBREQUESTWAITINGONSOCKET";
				analyzedThreadClass.Description = "making an HttpWebRequest and waiting on the remote server to respond";
				analyzedThreadClass.Recommendation = "If many threads are in this state, it is often an indication that the remote server is not responding properly or there is some kind of a network issue. Click on any thread in the list to the left to review the details of the WebRequest on which it is waiting";
			}
			else
			{
				analyzedThreadClass.Category = "DOTNETWAITINGONWEBREQUEST";
				analyzedThreadClass.Description = "attempting to make an HttpWebRequest, however they do <b>*not*</b> appear to be waiting on the remote server to respond (eg. not 'on the wire')";
				analyzedThreadClass.Recommendation = "If many threads are in this state, it is often an indication that a throttling limit (i.e. the 'maxconnection' setting) has been exhausted. Click on any thread in the list to the left to review the throttling details for the WebRequest on which it is waiting";
			}
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass.IsWarning = true;
			obj19 = FindObjectFromDSO("System.Net.HttpWebRequest", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj19))
			{
				text9 = DumpObject(obj19, "_ServicePoint");
				if (text9 != Globals.NOT_FOUND)
				{
					obj20 = DumpLong(text9, "m_ConnectionLimit");
					obj21 = DumpLong(text9, "m_CurrentConnections");
					text8 = Convert.ToString(DumpString(obj19, "_ConnectionGroupName"));
					text8 = ((!(text8 != Convert.ToString(Globals.NOT_FOUND))) ? "" : ("<br>ConnectionGroupName: " + text8));
					if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) >= 2)
					{
						num8 = Convert.ToInt32(DumpByte(text9, "m_IPAddressesAreLoopback"));
						num9 = Convert.ToInt32(DumpByte(text9, "m_UserChangedLimit"));
						if (obj20 == Globals.NOT_FOUND || obj21 == Globals.NOT_FOUND)
						{
							analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='green'> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8;
							if (Convert.ToString(num8) == "1" && Convert.ToString(num9) == "0")
							{
								analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br><br> The HttpWebRequest object is a loopback address so the ConnectionLimit is ignored</font><br><br>";
							}
							else
							{
								analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "</font><br><br>";
							}
						}
						else
						{
							num8 = Convert.ToInt32(num8);
							num9 = Convert.ToInt32(num9);
							flag4 = false;
							switch (num8)
							{
							case 0:
								flag4 = true;
								break;
							case 1:
								if (num9 == 1)
								{
									flag4 = true;
								}
								break;
							}
							if (Convert.ToDouble(obj21) >= Convert.ToDouble(obj20) && flag4)
							{
								if (flag3)
								{
									analyzedThreadClass.Category = "DOTNETEXCEEDMAXCONNECTIONSSOCKET";
									if (Globals.g_SysNetServicePointsWaitingOnSocket.ContainsKey(text9))
									{
										Globals.g_SysNetServicePointsWaitingOnSocket[text9] = Convert.ToInt32(Globals.g_SysNetServicePointsWaitingOnSocket[text9]) + 1;
									}
									else
									{
										Globals.g_SysNetServicePointsWaitingOnSocket.Add(text9, 1);
									}
								}
								else
								{
									analyzedThreadClass.Category = "DOTNETEXCEEDMAXCONNECTIONS";
									if (!Globals.g_SysNetServicePointsWaitingOnSocket.ContainsKey(text9))
									{
										Globals.g_SysNetServicePointsWaitingOnSocket.Add(text9, 0);
									}
								}
								analyzedThreadClass.IsError = true;
								analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + ". <font color='red'>One or more of these requests are exceeding its maximum number of available connections.</font>";
								analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='red'><b>The thread is exceeding the number of available connections</b><br><br> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8;
								if (num8 == 1)
								{
									analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br><br> The HttpWebRequest object is a loopback address but the connection limit still applies to this webrequest object because the connection limit is defined (either through autoconfig set to true in the processModel section or by adding a * entry inside connectionManagement section </font><br><br>";
								}
								else
								{
									analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "</font><br><br>";
								}
								analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + ". <br><br>If necessary, you can increase the number of connections available by either modifying the 'maxconnection' parameter in the application configuration file (see <a TARGET=_blank href ='http://msdn.microsoft.com/en-us/library/aa903351(VS.71).aspx'>&lt;connectionManagement&gt; Element</a>), or by modifying the appropriate ConnectionLimit property programmatically (see <a TARGET=_blank href='http://msdn.microsoft.com/en-us/library/7af54za5.aspx'>Managing Connections</a>).";
							}
							else if (Convert.ToDouble(Convert.ToInt32(obj21) * 2) >= Convert.ToDouble(obj20) && flag4)
							{
								if (flag3)
								{
									analyzedThreadClass.Category = "DOTNETABOUTTOEXCEEDMAXCONNECTIONSSOCKET";
								}
								else
								{
									analyzedThreadClass.Category = "DOTNETABOUTTOEXCEEDMAXCONNECTIONS";
								}
								analyzedThreadClass.IsWarning = true;
								analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + ". <font color='orange'>One or more of these requests are using at least half of its maximum number of available connections.</font>";
								analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='orange'><b>Warning, at least half of the availabe connections are being used</b><br><br> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8;
								if (num8 == 1)
								{
									analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br><br> The HttpWebRequest object is a loopback address but the connection limit still applies to this webrequest object because the connection limit is defined (either through autoconfig set to true in the processModel section or by adding a * entry inside connectionManagement section </font><br><br>";
								}
								else
								{
									analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "</font><br><br>";
								}
								analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + ". <br><br>If necessary, you can increase the number of connections available by either modifying the 'maxconnection' parameter in the application configuration file (see <a TARGET=_blank href ='http://msdn.microsoft.com/en-us/library/aa903351(VS.71).aspx'>&lt;connectionManagement&gt; Element</a>), or by modifying the appropriate ConnectionLimit property programmatically (see <a TARGET=_blank href='http://msdn.microsoft.com/en-us/library/7af54za5.aspx'>Managing Connections</a>).";
							}
							else
							{
								analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='green'> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8;
								if (num8 == 1)
								{
									if (flag4)
									{
										analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br><br> The HttpWebRequest object is a loopback address but the connection limit still applies to this webrequest object because the connection limit is defined (either through autoconfig set to true in the processModel section or by adding a * entry inside connectionManagement section </font><br><br>";
									}
									else
									{
										analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br><br> The HttpWebRequest object is a loopback address so the ConnectionLimit is ignored</font><br><br>";
									}
								}
								else
								{
									analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "</font><br><br>";
								}
							}
						}
						obj22 = FindObjectFromDSO("System.Net.Connection", Thread.ThreadID, true);
						if (!Globals.HelperFunctions.IsNullOrEmpty(obj22) && Convert.ToBoolean(Globals.g_SysNetConnectionWithBuffer.ContainsKey(Globals.HelperFunctions.FromHex((string)obj22))))
						{
							analyzedThreadClass.Category = Convert.ToString(analyzedThreadClass.Category) + "BUFFER";
							analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + "<br><br>The <b>read buffer</b> contains the response from a previous request too.";
							text10 = Convert.ToString(Globals.g_SysNetConnectionWithBuffer[Globals.HelperFunctions.FromHex((string)obj22)]);
							text10 = Strings.Replace(text10, "\n", "<br>");
							analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<b>Connection Buffer</b><br/>" + text10;
						}
					}
					else if (Convert.ToInt32(Globals.g_Debugger.ClrVersionInfo.Version.Major) == 1)
					{
						if (obj20 == Globals.NOT_FOUND || obj21 == Globals.NOT_FOUND)
						{
							analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='green'> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8 + "</font><br><br>";
						}
						else if (Convert.ToDouble(obj21) >= Convert.ToDouble(obj20))
						{
							if (flag3)
							{
								analyzedThreadClass.Category = "DOTNETEXCEEDMAXCONNECTIONSSOCKET";
							}
							else
							{
								analyzedThreadClass.Category = "DOTNETEXCEEDMAXCONNECTIONS";
							}
							analyzedThreadClass.IsError = true;
							analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + ". <font color='red'>One or more of these requests are exceeding its maximum number of available connections.</font>";
							analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='red'><b>The thread is exceeding the number of available connections</b><br><br> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8 + "</font><br><br>";
							analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + ". <br><br>If necessary, you can increase the number of connections available by either modifying the 'maxconnection' parameter in the application configuration file (see <a TARGET=_blank href ='http://msdn.microsoft.com/en-us/library/aa903351(VS.71).aspx'>&lt;connectionManagement&gt; Element</a>), or by modifying the appropriate ConnectionLimit property programmatically (see <a TARGET=_blank href='http://msdn.microsoft.com/en-us/library/7af54za5.aspx'>Managing Connections</a>).";
						}
						else if (Convert.ToDouble(Convert.ToInt32(obj21) * 2) >= Convert.ToDouble(obj20))
						{
							if (flag3)
							{
								analyzedThreadClass.Category = "DOTNETABOUTTOEXCEEDMAXCONNECTIONSSOCKET";
							}
							else
							{
								analyzedThreadClass.Category = "DOTNETABOUTTOEXCEEDMAXCONNECTIONS";
							}
							analyzedThreadClass.IsWarning = true;
							analyzedThreadClass.Description = Convert.ToString(analyzedThreadClass.Description) + ". <font color='orange'>One or more of these requests are using at least half of its maximum number of available connections.</font>";
							analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='orange'><b>Warning, at least half of the availabe connections are being used</b><br><br> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8 + "</font><br><br>";
							analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + ". <br><br>If necessary, you can increase the number of connections available by either modifying the 'maxconnection' parameter in the application configuration file (see <a TARGET=_blank href ='http://msdn.microsoft.com/en-us/library/aa903351(VS.71).aspx'>&lt;connectionManagement&gt; Element</a>), or by modifying the appropriate ConnectionLimit property programmatically (see <a TARGET=_blank href='http://msdn.microsoft.com/en-us/library/7af54za5.aspx'>Managing Connections</a>).";
						}
						else
						{
							analyzedThreadClass.AdditionalCLRInfo = "<br> <font color='green'> HttpRequest URI:" + Convert.ToString(getUriString(obj19)) + "<br>ServicePoint - ConnectionLimit:" + Convert.ToString(obj20) + " CurrentConnections:" + Convert.ToString(obj21) + text8 + "</font><br><br>";
						}
					}
				}
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if ((Convert.ToString(clrStackReportNoArgs).IndexOf("System.Threading.Thread.Sleep") >= 0 || Thread.FindFrameInStack("THREADNATIVE::SLEEP") > -1) && !Convert.ToBoolean(CheckInClrStackIfSleepIsCozMSFT(Thread)))
		{
			int num10 = 0;
			string text11 = "";
			analyzedThreadClass.Category = "DOTNETWAITINGINTHREADSLEEP";
			analyzedThreadClass.Description = "making a call to Sleep API using the .net Library</b>";
			analyzedThreadClass.Recommendation = "";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			dictionary = Thread.StackFrames;
			if (Convert.ToInt32(Globals.g_SizeOfULongPtr) == 8)
			{
				analyzedThreadClass.Recommendation = "The duration of the Sleep call is unavailable. Please look at the callstack and the code of the function that is calling Sleep to determine the actual time the thread is sleeping.";
			}
			else
			{
				int num11 = Thread.FindFrameInStack("KERNEL32!SLEEPEX");
				if (num11 <= -1)
				{
					num11 = Thread.FindFrameInStack("KERNEL32!SLEEP");
					if (num11 <= -1)
					{
						num11 = Thread.FindFrameInStack("KERNELBASE!SLEEPEX");
					}
				}
				num10 = 0;
				if (num11 > -1)
				{
					num10 = (int)((ulong)dictionary[num11].Args(0) & 0xFFFFFFFFu);
					text11 = "<br>The duration of the Sleep call is <b>";
					if (num10 < 1000)
					{
						text11 = text11 + Convert.ToString(num10) + " miliseconds</b>.  Short calls to the Sleep API often occur inside of a tight loop, which will delay the application and cause high CPU until the loop is exited.";
					}
					else
					{
						text11 = ((num10 >= 60000) ? (text11 + Convert.ToString(num10 / 60000) + " minutes</b>.  ") : (text11 + Convert.ToString((double)num10 / 1000.0) + " seconds</b>.  "));
						text11 += "Long calls to the Sleep API can delay the application significantly. ";
					}
					analyzedThreadClass.Recommendation = text11;
				}
				analyzedThreadClass.Category = Convert.ToString(analyzedThreadClass.Category) + Convert.ToString(num10);
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("WAITONE") > -1 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.ServiceModel.Activation.HostedHttpRequestAsyncResult.ExecuteSynchronous") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGFORWCFIOTHREAD";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting on a synchronous WCF request to execute";
			analyzedThreadClass.Recommendation = "Please click on the Actual Thread to see the desnitation WCF Thread or check <a href='#WCFREQUEST" + Convert.ToString(Globals.g_UniqueReference) + "'>WCF Request report </a> to find out the thread id for the actual WCF thread for which this thread is waiting on. For more details on this WCF Behavior check out the blog on <a href='http://blogs.msdn.com/b/wenlong/archive/2008/04/21/wcf-request-throttling-and-server-scalability.aspx'>WCF Request Throttling and Server Scalability</a> ";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			num = 0.0;
			foreach (double key in Globals.g_HttpContextThreads.Keys)
			{
				if (Globals.g_HttpContextThreads[key] == Thread.ThreadID)
				{
					num = num3;
					break;
				}
			}
			if (num != 0.0 && Globals.g_WCFIOSchedulerThreads.ContainsKey(num))
			{
				num2 = Globals.g_WCFIOSchedulerThreads[num];
				analyzedThreadClass.AdditionalCLRInfo = "The destination WCF Thread for this ASP.NET worker thread is " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(num2)) + "<br/>";
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("WAITONE") > -1)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGINWAITONE";
			analyzedThreadClass.Description = "waiting in a WaitOne";
			analyzedThreadClass.Recommendation = "Typically threads waiting in WaitMultiple are monitoring threads in a process and this may be ignored, however too many threads waiting in WaitOne\\ WaitMultiple may be a problem. Review the callstack of the threads waiting to see what they are waiting on";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.String.Concat") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETSTRINGCONCATENATION";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "concatenating strings";
			analyzedThreadClass.Recommendation = "Use a StringBuilder and append data rather than concatenating strings. For more details refer to <a href='http://blogs.msdn.com/b/tess/archive/2008/02/27/net-debugging-demos-lab-4-high-cpu-hang-review.aspx'>.NET Debugging Demos Lab 4: High CPU Hang - Review</a> and <a href='http://blogs.msdn.com/b/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if ((Thread.FindFrameInStack("NTTERMINATEPROCESS") > -1 || Thread.FindFrameInStack("TERMINATEPROCESS") > -1) && Thread.FindFrameInStack("HANDLESTACKOVERFLOW") > -1 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.Data.BinaryNode.Eval") > 0 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.Data.BinaryNode.EvalBinaryOp") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETSTACKOVERFLOWDUETOCOMPLEXROWFILTER";
			analyzedThreadClass.IsError = true;
			analyzedThreadClass.Description = "probably using complex row-filters or expressions on DataSets\\DataTables and ending up in a fatal <font color='red'><b>StackOverFlowException</b></font> and this is causing the process to <b>Terminate</b>";
			analyzedThreadClass.Recommendation = "Please check if the expression was really meant to be that long or if there is a logic issue which is causing the expression to grow that big. If you really need an extremely long expression you may spin a new thread (with a bigger callstack) to perform the setting of the rowfilter. <br><br>More details about this issue in <a href='http://blogs.msdn.com/b/tess/archive/2008/03/31/net-case-study-stackoverflow-exception-when-using-a-complex-rowfilter.aspx'>.NET Case Study: Stackoverflow Exception when using a complex rowfilter</a>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			flag = false;
			obj2 = FindObjectFromDSO("System.Data.DataExpression", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj2))
			{
				obj3 = DumpString(obj2, "originalExpression");
				if (obj3 != Globals.NOT_FOUND)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				obj = FindObjectFromDSO("System.Data.Select", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj))
				{
					obj2 = DumpObject(obj, "rowFilter");
					if (obj2 != Globals.NOT_FOUND)
					{
						obj3 = DumpString(obj2, "originalExpression");
						if (obj3 != Globals.NOT_FOUND)
						{
							flag = true;
						}
					}
				}
			}
			if (flag)
			{
				analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + "<br><br> <b>Note:</b> Click on the Thread-Id to get more details about the expression object used on this thread.";
				analyzedThreadClass.AdditionalCLRInfo = "The DataExpression used for the RowFilter is <b><font color='blue'>" + Convert.ToString(obj3) + "</font></b><br><br>";
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Collections.Generic.Dictionary`2") >= 0 && Convert.ToString(clrStackReportNoArgs).IndexOf("FindEntry") >= 0)
		{
			array = Strings.Split(Convert.ToString(clrStackReportNoArgs), "\n");
			for (num4 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num4 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num4++)
			{
				if (array[num4].IndexOf("System.Collections.Generic.Dictionary`2") >= 0 && array[num4].IndexOf("FindEntry") >= 0)
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				analyzedThreadClass.Category = "DOTNETSTUCKINGENERICDICTIONARY";
				analyzedThreadClass.IsWarning = true;
				analyzedThreadClass.Description = "enumerating a <b>System.Collections.Generic.Dictionary</b> object";
				analyzedThreadClass.Recommendation = "Multiple threads enumerating through a collection is intrinsically not a thread-safe procedure. If the dictionary object accessed by these threads is declared as <B>static</b> then the threads can go in an infinite loop while trying to enumerate the dictionary if one of the threads writes to the dictionary while the other threads are reading\\enumerating through the same dictionary. You may also experience High CPU during this stage. For more details refer to <a href='http://blogs.msdn.com/b/tess/archive/2009/12/21/high-cpu-in-net-app-using-a-static-generic-dictionary.aspx'>High CPU in .NET app using a static Generic.Dictionary</a>";
				analyzedThreadClass.KeyPartOne = "0";
				analyzedThreadClass.KeyPartTwo = "0";
				analyzedThreadClass2 = analyzedThreadClass;
			}
			else
			{
				analyzedThreadClass2 = null;
			}
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Security.Policy.PolicyLevel.Load") >= 0 && Convert.ToString(clrStackReportNoArgs).IndexOf("System.Security.Util.Parser..ctor") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETRESOLVINGPOLICY";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "trying to resolve a policy";
			analyzedThreadClass.Recommendation = "Please check if you running in to the issue described in <a href='http://blogs.msdn.com/b/tess/archive/2008/03/26/asp-net-case-study-hang-with-mixed-mode-dlls.aspx'>ASP.NET Case Study: Hang with mixed-mode dlls</a>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Text.RegularExpressions.Regex.Match") >= 0 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Text.RegularExpressions.Regex.Run") >= 0 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Text.RegularExpressions.RegexRunner.Scan") >= 0 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Text.RegularExpressions.RegexInterpreter.Go") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETREGULAREXPRESSION";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "using regular expressions";
			analyzedThreadClass.Recommendation = "Badly formatted regular expressions can end up in tight loops and can cause high CPU. Please refer to  <a href='http://blogs.msdn.com/b/tess/archive/2007/10/25/net-hang-case-study-high-cpu-because-of-poorly-formatted-regular-expressions-identifying-tight-loops.aspx'>.NET Hang case study - High CPU because of poorly formatted regular expressions (Identifying tight loops)</a> for more details on this issue";
			obj4 = FindObjectFromDSO("System.Text.RegularExpressions.RegexInterpreter", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj4))
			{
				analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br/><b>RegEx Match String : </b>" + Convert.ToString(HTMLEncode(DumpString(obj4, "runtext")));
				analyzedThreadClass.AdditionalCLRInfo = Convert.ToString(analyzedThreadClass.AdditionalCLRInfo) + "<br/><br/><b>RegEx Pattern : </b>" + Convert.ToString(HTMLEncode(DumpString(DumpObject(obj4, "runregex"), "pattern"))) + "</br><br/>";
			}
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Threading.ReaderWriterLock.AcquireWriterLockInternal") >= 0 || Thread.FindFrameInStack("CRWLOCK::STATICACQUIREWRITERLOCKPUBLIC") > -1)
		{
			analyzedThreadClass.Category = "DOTNETACQUIREREADERLOCKWRITER";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "trying to acquire a System.Threading.ReaderWriterLock for <b>writing</b>";
			analyzedThreadClass.Recommendation = "Further manual debugging needs to be done to identify the root cause";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Threading.ReaderWriterLock.AcquireReaderLockInternal") >= 0 || Thread.FindFrameInStack("CRWLOCK::STATICACQUIREREADERLOCKPUBLIC") > -1)
		{
			analyzedThreadClass.Category = "DOTNETACQUIREREADERLOCKREADER";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "trying to acquire a System.Threading.ReaderWriterLock for <b>reading</b>";
			analyzedThreadClass.Recommendation = "Further manual debugging needs to be done to identify the root cause";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("httpapi!HttpReceiveRequestEntityBody".ToUpper()) > -1)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGONHTTPSYS";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "waiting on client to send more data";
			analyzedThreadClass.Recommendation = "There can be multiple reasons why a thread is waiting on client  <br> <ol><li>The client sent a lot of data in the entity-body and the server is still reading it. (Click on the thread number on the left and see the details on the request in the actual thread report to see if this is the case).</li><li>Network issues between the client and the server causing delayed or dropped TCP packets .</li><li>Filter drivers, such as antivirus scanners, may be scanning inbound data causing delayed/blocked transfer  .</li></ol>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			string text12 = FindObjectFromDSO("System.Web.HttpRawUploadedContent", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(text12))
			{
				int num12 = (int)DumpLong(text12, "_expectedLength");
				int num13 = (int)DumpLong(text12, "_length");
				if (num13 != 0 && num12 != 0)
				{
					analyzedThreadClass.AdditionalCLRInfo = "The thread has read <b> " + Convert.ToString(num13) + " bytes</b> out of a total of <b>" + Convert.ToString(num12) + " bytes</b> in the HTTP Request<br><br>";
				}
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Thread.FindFrameInStack("httpapi!HttpSendResponseEntityBody".ToUpper()) > -1 || Thread.FindFrameInStack("httpapi!HttpSendHttpResponse".ToUpper()) > -1)
		{
			analyzedThreadClass.Category = "DOTNETSENDINGDATATOHTTPSYS";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "sending entity-body data associated with an HTTP response to HTTP.SYS";
			analyzedThreadClass.Recommendation = "There can be multiple reasons why a thread is waiting to send data to the client  <br> <ol><li>There is a lot of data in the entity-body and the server is still sending it. (Click on the thread number on the left and see the details on the request in the actual thread report to see if this is the case).</li><li>Network issues between the client and the server causing delayed or dropped TCP packets .</li><li>Filter drivers, such as antivirus scanners, may be scanning outbound data causing delayed/blocked transfer  .</li>";
			if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.HttpResponse.Flush") >= 0)
			{
				analyzedThreadClass.Category = "DOTNETSENDINGDATATOHTTPSYSCALLINGFLUSH";
				analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + "<li>The thread is calling the Response.Flush method which sends output up to that point in ResponseBuffer to the client. If the client connects over slow networks then this can affect the response time of the server because the server needs to wait for acknowledgements from the client.</li><li>Another reason could be that the code is calling Response.Flush\\Response.BinaryWrite in a loop which typically happens when reading files in memory streams and calling Response.Flush in chuncks. If this is the case, use Response.TransmitFile method to transfer files to the client.</li>";
			}
			analyzedThreadClass.Recommendation = Convert.ToString(analyzedThreadClass.Recommendation) + "</ol>";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Runtime.Remoting.Proxies.RemotingProxy.InternalInvoke") >= 0)
		{
			analyzedThreadClass.Category = "DOTNETCALLINGREMOTING";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			if (Thread.FindFrameInStack("WS2_32!") > -1 || Thread.FindFrameInStack("MSWSOCK!") > -1)
			{
				analyzedThreadClass.Category = "DOTNETCALLINGREMOTINGWAITINGONSOCKET";
				analyzedThreadClass.Description = "making a .NET remoting call and waiting on the remote server to respond";
				analyzedThreadClass.Recommendation = "If many threads are in this state, it is often an indication that the remote server is not responding properly or there is some kind of a network issue. Click on any thread in the list to the left to review the URI of the <b>Remoting Server</b> on which it is waiting.";
			}
			else
			{
				analyzedThreadClass.Category = "DOTNETCALLINGREMOTING";
				analyzedThreadClass.Description = "attempting to make a .NET Remoting call, however they do <b>*not*</b> appear to be waiting on the remote server to respond (eg. not 'on the wire')";
				analyzedThreadClass.Recommendation = "If many threads are in this state, it is often an indication that a throttling limit has been exhausted. Click on any thread in the list to the left to review the URI of the <b>Remoting Server</b> on which it is waiting.";
			}
			string text13 = FindObjectFromDSO("System.Runtime.Remoting.Messaging.Message", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(text13))
			{
				string text14 = DumpString(text13, "_URI");
				if (text14 != Globals.NOT_FOUND)
				{
					analyzedThreadClass.AdditionalCLRInfo = "<font color='green'> Remote URI:" + Convert.ToString(text14) + "</font><br><br>";
				}
			}
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else if (Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.HttpValueCollection.FillFromEncodedBytes") >= 0 || Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.HttpRequest.FillInFormCollection") >= 0 || Thread.FindFrameInStack("System.Web.HttpValueCollection.FillFromEncodedBytes".ToUpper()) > -1 || Thread.FindFrameInStack("System.Web.HttpRequest.FillInFormCollection".ToUpper()) > -1)
		{
			int num14 = 0;
			int num15 = 0;
			bool flag5 = false;
			object obj23 = null;
			object obj24 = null;
			bool flag6 = false;
			int num16 = 0;
			string text15 = "";
			num16 = 1;
			flag5 = false;
			num14 = 0;
			num15 = 0;
			obj24 = FindObjectFromDSO("System.Web.HttpRawUploadedContent", Thread.ThreadID, true);
			if (!Globals.HelperFunctions.IsNullOrEmpty(obj24))
			{
				flag6 = (bool)DumpByte(obj24, "_completed");
				if (!Globals.HelperFunctions.IsNullOrEmpty(flag6))
				{
					flag5 = true;
				}
			}
			else
			{
				obj23 = FindObjectFromDSO("System.Web.HttpRequest", Thread.ThreadID, true);
				if (!Globals.HelperFunctions.IsNullOrEmpty(obj23))
				{
					obj24 = DumpObject(obj23, "_rawContent");
					if (!Globals.HelperFunctions.IsNullOrEmpty(obj24) && obj24 != Globals.NOT_FOUND)
					{
						flag6 = (bool)DumpByte(obj24, "_completed");
						if (!Globals.HelperFunctions.IsNullOrEmpty(flag6))
						{
							flag5 = true;
						}
					}
				}
			}
			num14 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.HttpValueCollection.FillFromEncodedBytes");
			if (0 >= num14)
			{
				num14 = Convert.ToInt32(Thread.FindFrameInStack("System.Web.HttpValueCollection.FillFromEncodedBytes".ToUpper()));
				if (Convert.ToInt32(-1) == num14)
				{
					num14 = 0;
				}
			}
			if (0 >= num14)
			{
				num14 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.Web.HttpRequest.FillInFormCollection");
				if (0 >= num14)
				{
					num14 = Convert.ToInt32(Thread.FindFrameInStack("System.Web.HttpRequest.FillInFormCollection".ToUpper()));
					if (Convert.ToInt32(-1) == num14)
					{
						num14 = 0;
					}
				}
			}
			else
			{
				flag6 = true;
				flag5 = true;
			}
			num15 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.StringComparer.Equals");
			if (0 >= num15)
			{
				num15 = Convert.ToInt32(Thread.FindFrameInStack("System.StringComparer.Equals".ToUpper()));
				if (Convert.ToInt32(-1) == num15)
				{
					num15 = 0;
				}
			}
			if (0 >= num15)
			{
				num15 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.Collections.Hashtable");
				if (0 >= num15)
				{
					num15 = Convert.ToInt32(Thread.FindFrameInStack("System.Collections.Hashtable".ToUpper()));
					if (Convert.ToInt32(-1) == num15)
					{
						num15 = 0;
					}
				}
			}
			if (0 >= num15)
			{
				num15 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.Collections.Specialized.NameObjectCollectionBase");
				if (0 >= num15)
				{
					num15 = Convert.ToInt32(Thread.FindFrameInStack("System.Collections.Specialized.NameObjectCollectionBase".ToUpper()));
					if (Convert.ToInt32(-1) == num15)
					{
						num15 = 0;
					}
				}
			}
			if (0 >= num15)
			{
				num15 = Convert.ToString(clrStackReportNoArgs).IndexOf("System.Collections.Specialized.NameValueCollection");
				if (num15 == 0)
				{
					num15 = Convert.ToInt32(Thread.FindFrameInStack("System.Collections.Specialized.NameValueCollection".ToUpper()));
					if (Convert.ToInt32(-1) == num15)
					{
						num15 = 0;
					}
				}
			}
			if (flag6)
			{
				num16 = 2;
			}
			else if (num15 != 0 && num15 < num14 && flag5)
			{
				num16 = (flag6 ? 2 : 0);
			}
			if (num16 > 0)
			{
				if (num16 > 1)
				{
					analyzedThreadClass.IsWarning = false;
					analyzedThreadClass.IsError = true;
				}
				else
				{
					analyzedThreadClass.IsWarning = true;
					analyzedThreadClass.IsError = false;
				}
				analyzedThreadClass.Description = "busy filling in the Form collection from the entity body of the request, which indicates a problem in the number of form variables, and a possible Denial of Service attack.";
				analyzedThreadClass.Recommendation = "This is caused by a POST request containing an extremely large number of form variables whose key and value pairs cause collisions in the underlying hash table. Please see the following Microsoft Security Advisory for more details on this issue and its resolution.<P><a href=" + Convert.ToString('"') + "http://technet.microsoft.com/en-us/security/advisory/2659883" + Convert.ToString('"') + ">http://technet.microsoft.com/en-us/security/advisory/2659883</a>";
				analyzedThreadClass2 = analyzedThreadClass;
				text15 = "<a href=" + Convert.ToString('"') + "#" + Convert.ToString(Globals.g_UniqueReference) + "Thread" + Convert.ToString(Thread.SystemID) + Convert.ToString('"') + ">" + Convert.ToString(Thread.ThreadID) + "</a>";
				Globals.Manager.ReportError("Thread " + text15 + " is " + Convert.ToString(analyzedThreadClass.Description), analyzedThreadClass.Recommendation, 1000, "{b4316888-7df1-489f-97dc-5057fa4f734d}");
				analyzedThreadClass.IsWarning = false;
				analyzedThreadClass.IsError = false;
				analyzedThreadClass.Description = "";
				analyzedThreadClass.Recommendation = "";
				analyzedThreadClass2 = null;
			}
			else
			{
				analyzedThreadClass.IsWarning = false;
				analyzedThreadClass.IsError = false;
				analyzedThreadClass.Description = "";
				analyzedThreadClass.Recommendation = "";
				analyzedThreadClass2 = null;
			}
		}
		else if (Thread.FindFrameInStack("WAITMULTIPLE") > -1)
		{
			analyzedThreadClass.Category = "DOTNETWAITINGINWAITMULTIPLE";
			analyzedThreadClass.Description = "waiting in a WaitMultiple";
			analyzedThreadClass.Recommendation = "Typically threads waiting in WaitMultiple are monitoring threads in a process and this may be ignored, however too many threads waiting in WaitOne\\ WaitMultiple may be a problem. Review the callstack of the threads waiting to see what they are waiting on";
			analyzedThreadClass.KeyPartOne = "0";
			analyzedThreadClass.KeyPartTwo = "0";
			analyzedThreadClass2 = analyzedThreadClass;
		}
		else
		{
			analyzedThreadClass2 = null;
		}
		analyzedThreadClass2?.AdjustCategoryIfHasWaiters();
		return analyzedThreadClass2;
	}

	public string ParseChainOutput()
	{
		string text = "";
		string[] array = null;
		int num = 0;
		string text2 = "";
		string[] array2 = null;
		text = "";
		array = Strings.Split(Convert.ToString((object)Globals.g_Debugger.Execute(".chain")), "\n");
		for (num = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num++)
		{
			text2 = array[num].TrimStart();
			if (text2.GetSafeLength() >= 6 && text2.Substring(0, 6) == "[path:" && (text2.IndexOf("sos") >= 0 || text2.IndexOf("psscor") >= 0))
			{
				array2 = Strings.Split(text2, ": ");
				if (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1) > 0)
				{
					text = array2[1].Substring(0, array2[1].GetSafeLength() - 1);
				}
			}
		}
		return text;
	}

	public string FindObjectFromDSO(string objectType, int intThreadId, object stopOnFirstObject)
	{
		string[] array = null;
		string text = "";
		string[] array2 = null;
		object obj = null;
		obj = null;
		if (Convert.ToInt32(Globals.g_DumpStackObjects.Count) == 0)
		{
			PopulateDSOCache();
		}
		if (Globals.g_DumpStackObjects.ContainsKey(intThreadId))
		{
			string text2 = Globals.g_DumpStackObjects[intThreadId];
			if (text2.IndexOf(objectType) >= 0)
			{
				array = Strings.Split(Convert.ToString(text2), "\n");
				text = "";
				for (int i = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1) + 2; i <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1) - 1; i++)
				{
					if (!(array[i].Trim() != ""))
					{
						continue;
					}
					array[i] = array[i].Replace("\r", "");
					if (!array[i].EndsWith(objectType))
					{
						continue;
					}
					text = array[i];
					text = Convert.ToString(normalizeWhitespace(text));
					array2 = Strings.Split(text);
					if (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1) > 1)
					{
						if (Convert.ToBoolean(stopOnFirstObject))
						{
							return array2[1];
						}
						if (Globals.HelperFunctions.IsNullOrEmpty(obj))
						{
							obj = array2[1];
						}
						else if (Convert.ToString(obj).ToUpper().IndexOf(array2[1].ToUpper()) < 0)
						{
							obj = Convert.ToString(obj) + "," + array2[1];
						}
					}
				}
			}
			else
			{
				obj = null;
			}
		}
		return Convert.ToString(obj);
	}

	public AnalyzedThreadClass RandomKnownIssues(CacheFunctions.ScriptThreadClass Thread, object FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		AnalyzedThreadClass analyzedThreadClass2 = new AnalyzedThreadClass();
		analyzedThreadClass2.Thread = Thread;
		if (Thread.FindFrameInStack("FINDNEXTFILEW") > -1 && Thread.FindFrameInStack("PROCESSISAPIREQUEST") > -1 && Thread.FindFrameInStack("CPERFCOUNTERSERVER::INITIALIZE") > -1)
		{
			analyzedThreadClass2.Category = "DOTNETAPPLICATIONHANGSONSTARTUP";
			analyzedThreadClass2.IsWarning = true;
			analyzedThreadClass2.Description = "processing an ISAPI request and trying to initialize some performance counters";
			analyzedThreadClass2.Recommendation = "Please check if you are running in to the issue described in <a href='http://blogs.msdn.com/b/tess/archive/2007/08/13/asp-net-hang-case-study-application-hangs-on-startup.aspx'>ASP.NET Hang Case Study: Application hangs on startup</a>";
			analyzedThreadClass2.KeyPartOne = "0";
			analyzedThreadClass2.KeyPartTwo = "0";
			return analyzedThreadClass2;
		}
		if (Thread.FindFrameInStack("ADVAPI322!LOOKUPACCOUNTNAMEW") > -1 && Thread.FindFrameInStack("ISAPI_DLL::LOAD") > -1 && Thread.FindFrameInStack("RPCRT4!OSF_CCALL") > -1)
		{
			analyzedThreadClass2.Category = "DOTNETAPPLICATIONHANGSLOOKUPACCOUNT";
			analyzedThreadClass2.IsWarning = true;
			analyzedThreadClass2.Description = "trying to lookup some account names while trying to initialize some performance counters";
			analyzedThreadClass2.Recommendation = "Please check if you are running in to the issue described in <a href='http://blogs.msdn.com/b/tess/archive/2008/01/17/asp-net-hang-slowness-on-startup.aspx'>ASP.NET hang/slowness on startup</a>";
			analyzedThreadClass2.KeyPartOne = "0";
			analyzedThreadClass2.KeyPartTwo = "0";
			return analyzedThreadClass2;
		}
		if ((Thread.FindFrameInStack("KERNEL32!TERMINATEPROCESS") > -1 || Thread.FindFrameInStack("NTDLL!ZWTERMINATEPROCESS") > -1 || Thread.FindFrameInStack("NTDLL!NTTERMINATEPROCESS") > -1) && Convert.ToBoolean(Globals.g_IsSystemWebApp))
		{
			if (Thread.FindFrameInStack("MSVCRT!EXIT") > -1 || Thread.FindFrameInStack("MSVCRT!__CRTEXITPROCESS") > -1 || Thread.FindFrameInStack("MSVCRT!DOEXIT") > -1)
			{
				analyzedThreadClass2.Category = "THREADCALLINGTERMINATEPROCESSSAFEEXIT";
				analyzedThreadClass2.IsWarning = true;
				analyzedThreadClass2.Description = "calling the <B>TerminateProcess</B> function to shutdown the process";
				analyzedThreadClass2.Recommendation = "This dump file got created as a result of a <font color='GREEN'>normal shutdown</font> of the process. The process is shutting down either due to an IISRESET or due to some kind of recycling or idle-shutdown. Please disable application pool recycling and idle-shutdown of the application pool to avoid getting dumps on these exits. For more details refer to <a href='http://blogs.msdn.com/b/tess/archive/2008/09/12/quick-debugging-tip-disable-health-monitoring-while-getting-crash-dumps.aspx'>Quick Debugging Tip: Disable apppool recyling while getting crash dumps</a> ";
				analyzedThreadClass2.KeyPartOne = "0";
				analyzedThreadClass2.KeyPartTwo = "0";
				return analyzedThreadClass2;
			}
			analyzedThreadClass2.Category = "THREADCALLINGTERMINATEPROCESS";
			analyzedThreadClass2.IsError = true;
			analyzedThreadClass2.Description = "calling the <B><font color='red'>TerminateProcess</font></B> function to kill the process";
			if (Thread.FindFrameInStack("EEPOLICY::HANDLEFATALSTACKOVERFLOW") > -1)
			{
				analyzedThreadClass2.Recommendation = "The process crashed due to a <B><font color ='red'>Fatal Stack Overflow</font></B> <br/>";
				if (Thread.FindFrameInClrStack("SYSTEM.WEB.HTTPSERVERUTILITY.TRANSFER") > -1)
				{
					analyzedThreadClass2.Recommendation = Convert.ToString(analyzedThreadClass2.Recommendation) + "<br/>There is also an indication that Server.Transfer was called on this thread.  If you are calling Server.Transfer(same page, true) without additional code, a stack overflow occurs and this behavior is by design. Please refer to the following KB article for more information.<br/><a href='http://support.microsoft.com/kb/839521'>The Server.Transfer method causes a stack overflow and causes the ASP.NET worker process to stop responding</a><br/><br/>In case you are not calling Server.Transfer(same page, true), please look at the callstack of the thread to see what it is doing which is ending up in a Stack Overflow.";
				}
				else
				{
					analyzedThreadClass2.Recommendation = Convert.ToString(analyzedThreadClass2.Recommendation) + "<br/>Please look at the callstack of the thread to see what it is doing which is ending up in a Stack Overflow.";
				}
			}
			else
			{
				analyzedThreadClass2.Recommendation = "Please look at the callstack of the thread to see what it is doing which is ending up in a call to TerminateProcess function";
			}
			analyzedThreadClass2.KeyPartOne = "0";
			analyzedThreadClass2.KeyPartTwo = "0";
			return analyzedThreadClass2;
		}
		return null;
	}

	public void GenerateADODotNetReport()
	{
		string[] array = null;
		int num = 0;
		object obj = null;
		int num2 = 0;
		int num3 = 0;
		object obj2 = null;
		object obj3 = null;
		object obj4 = null;
		object obj5 = null;
		object obj6 = null;
		object[] array2 = null;
		int num4 = 0;
		int num5 = 0;
		array = DumpHeapForType("System.Data.ProviderBase.DbConnectionPool");
		if (Globals.HelperFunctions.IsNullOrEmpty(array))
		{
			return;
		}
		Globals.g_ADODotNetConnectionsPresent = true;
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("ADODOTNET", (SectionType)0);
		val.Title = "ADO.NET Connections Report";
		val.Collapsible = true;
		val.Write("<table cellpadding=0 cellspacing=0 border=1 class=myCustomText> <th>Connection Type</th> <th>MaxPoolSize</th> <th>Connection String</th> <th>Current Connection Count</th> <th>Connections Open</th></tr>");
		for (num = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num++)
		{
			num2 = Convert.ToInt32(DumpShort(array[num], "_totalObjects"));
			obj = DumpObject(array[num], "_connectionPoolGroupOptions");
			num3 = Convert.ToInt32(DumpShort(obj, "_maxPoolSize"));
			obj3 = DumpObject(array[num], "_connectionPoolGroup");
			obj2 = DumpObject(obj3, "_connectionOptions");
			obj4 = getManagedObjectType(obj2);
			obj6 = DumpObject(array[num], "_objectList");
			obj5 = DumpString(obj2, "_usersConnectionString");
			array2 = DumpObjectArray(obj6, "_items");
			num5 = 0;
			if (!Globals.HelperFunctions.IsNullOrEmpty(array2) && array2 != null)
			{
				for (num4 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array2, 1); num4 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1); num4++)
				{
					object obj7 = DumpByte(array2[num4], "_fConnectionOpen");
					if (obj7 != Globals.NOT_FOUND && Convert.ToInt32(obj7) == 1)
					{
						num5++;
					}
				}
			}
			val.Write("<tr>");
			val.Write("<td>" + Convert.ToString(obj4) + "</td><td>" + Convert.ToString(num3) + "</td><td>" + Convert.ToString(Globals.HelperFunctions.MaskPwd((string)obj5)) + "</td><td>" + Convert.ToString(num2) + "</td><td>" + Convert.ToString(num5) + "</td>");
			val.Write("</tr>");
		}
		val.Write("</table><br/><br/>");
	}

	public void CheckForWellKnownExceptionTypes(object ExceptionType, object ExceptionMessage, object ExceptionStack, object ExceptionStackRemote, object ExceptionCount)
	{
		string text = "";
		if (!(Convert.ToString(ExceptionType) == "System.InvalidOperationException"))
		{
			return;
		}
		if (!Convert.ToBoolean(Globals.g_WellKnownCLRExceptionsReported.ContainsKey(Convert.ToString(ExceptionType) + "The timeout period elapsed prior to obtaining a connection from the pool")))
		{
			text = "";
			text += "<br/><b><u>Exception Details</u></b><br/>";
			text = text + "<br><font color='red'><b>" + Convert.ToString(ExceptionType) + "</b></font><br><br>";
			text = text + Convert.ToString(ExceptionMessage) + "</font><br>";
			text = text + Convert.ToString(ExceptionStack) + "<br><br>";
			if (Convert.ToString(ExceptionMessage).IndexOf("The timeout period elapsed prior to obtaining a connection from the pool") >= 0)
			{
				Globals.Manager.ReportError("Exceptions indicating <font color='red'><b>ADO.NET database pool exhaustion</b></font> were detected in the dump file <br><br>" + text, "This exception indicates there are no more connections available in the connection pool either due to a connection leak or due to high load on the application. Review the  <a href='#ADODOTNET" + Convert.ToString(Globals.g_UniqueReference) + "'>ADO.NET Connections Report</a> to check the connection pooling information for this connection", 0, "{20e4142c-c81a-4f23-b778-b44bdb895e40}");
				Globals.g_WellKnownCLRExceptionsReported.Add(Convert.ToString(ExceptionType) + "The timeout period elapsed prior to obtaining a connection from the pool", (string)ExceptionMessage);
			}
		}
		else if (!Convert.ToBoolean(Globals.g_WellKnownCLRExceptionsReported.ContainsKey(Convert.ToString(ExceptionType) + "This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms")))
		{
			text = "";
			text += "<br/><b><u>Exception Details</u></b><br/>";
			text = text + "<br><font color='red'><b>" + Convert.ToString(ExceptionType) + "</b></font><br><br>";
			text = text + Convert.ToString(ExceptionMessage) + "</font><br>";
			text = text + Convert.ToString(ExceptionStack) + "<br><br>";
			if (Convert.ToString(ExceptionMessage).IndexOf("This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms") >= 0)
			{
				Globals.Manager.ReportError("A know exception was detected in the dump file <br><br>" + text, "The issue is caused when the group policy <b>System cryptography: Use FIPS compliant algorithms for encryption, hashing, and signing </b> is enabled on the web server. The registry key entry related to this group policy is: <b> HKLM\\System\\CurrentControlSet\\Control\\Lsa\\FIPSAlgorithmPolicy\\Enabled </b>. <br> To resolve this issue go to the properties of the website in the IIS manager and change the Encryption and Decryption Method to <b>Triple DES</b>.<br>More details on this issue in the <a href='http://support.microsoft.com/kb/981119'>KB981119</a>", 0, "{f6ad359a-a877-4f98-9b69-ae50b2923ec2}");
				Globals.g_WellKnownCLRExceptionsReported.Add(Convert.ToString(ExceptionType) + "This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms", (string)ExceptionMessage);
			}
		}
	}

	public bool IsWellKnownUnhandledException(object ExceptionType, object ExceptionMessage, object stackTrace, object remoteStackTraceString, CacheFunctions.ScriptThreadClass ExceptionThread, ref string AdditionalDescriptionString, ref string RecommendationString, ref string SolutionSourceID)
	{
		bool flag = false;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass2 = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass3 = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass4 = null;
		bool flag2 = false;
		string text = "";
		string text2 = "";
		flag = false;
		if (Convert.ToString(ExceptionType).IndexOf("System.TypeLoadException") >= 0)
		{
			if (Convert.ToString(ExceptionMessage).IndexOf("System.Security.Authentication.ExtendedProtection.ChannelBinding") >= 0)
			{
				flag2 = false;
				text = "";
				scriptModuleClass = Globals.g_ModuleCache.GetModuleByName("System_ni");
				scriptModuleClass2 = Globals.g_ModuleCache.GetModuleByName("System");
				scriptModuleClass3 = Globals.g_ModuleCache.GetModuleByName("System_Web");
				scriptModuleClass4 = Globals.g_ModuleCache.GetModuleByName("System_Web_ni");
				if (scriptModuleClass != null)
				{
					scriptModuleClass.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
					if (Convert.ToInt32(Major) == 2 && Convert.ToInt32(Priv) < 4205)
					{
						flag2 = true;
						text = text + "<br>System.ni.dll Version - " + Convert.ToString(Major) + "." + Convert.ToString(Minor) + "." + Convert.ToString(Build) + "." + Convert.ToString(Priv);
					}
				}
				if (scriptModuleClass2 != null)
				{
					scriptModuleClass2.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
					if (Convert.ToInt32(Major) == 2 && Convert.ToInt32(Priv) < 4205)
					{
						flag2 = true;
						text = text + "<br>System.dll Version - " + Convert.ToString(Major) + "." + Convert.ToString(Minor) + "." + Convert.ToString(Build) + "." + Convert.ToString(Priv);
					}
				}
				if (scriptModuleClass3 != null)
				{
					scriptModuleClass3.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
					text = text + "<br>System.Web.dll Version - " + Convert.ToString(Major) + "." + Convert.ToString(Minor) + "." + Convert.ToString(Build) + "." + Convert.ToString(Priv);
				}
				if (scriptModuleClass4 != null)
				{
					scriptModuleClass4.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
					text = text + "<br>System.Web.ni.dll Version - " + Convert.ToString(Major) + "." + Convert.ToString(Minor) + "." + Convert.ToString(Build) + "." + Convert.ToString(Priv);
				}
				if (flag2)
				{
					flag = true;
					RecommendationString = "This exception is happening because the System.Web assembly expects the type <b>System.Security.Authentication.ExtendedProtection.ChannelBinding</b> and System.DLL doesn't contain this type. This happens if the System.DLL version is lower than <b>2.0.50727.4205</b><br>" + text + "<br><br>To resolve this issue, please update the System.dll by applying the fix from <a href='http://support.microsoft.com/kb/980842'>http://support.microsoft.com/kb/980842</a> or apply the latest patch for System.DLL";
				}
			}
		}
		else if (Convert.ToString(ExceptionType).ToUpper() == "SYSTEM.OBJECTDISPOSEDEXCEPTION")
		{
			text2 = Convert.ToString(remoteStackTraceString).ToUpper();
			if (ExceptionThread.FindFrameInStack("MSCORWKS!RAISETHEEXCEPTIONINTERNALONLY") != -1 && ExceptionThread.FindFrameInStack("MSCORWKS!THREAD::RAISECROSSCONTEXTEXCEPTION") != -1 && text2.IndexOf("SYSTEM.SECURITY.PRINCIPAL.WIN32.IMPERSONATELOGGEDONUSER") > -1 && text2.IndexOf("SYSTEM.SECURITY.PRINCIPAL.WINDOWSIDENTITY.SAFEIMPERSONATE") > -1 && text2.IndexOf("SYSTEM.SECURITY.SECURITYCONTEXT.SETSECURITYCONTEXT") > -1)
			{
				flag = true;
				SolutionSourceID = "{89d8fe19-1789-4bfd-9da2-1ae79bc26d90}";
				AdditionalDescriptionString = "";
				RecommendationString = "This unhandled exception is due to a known issue with Exchange CAS server when it is used to proxy ActiveSync requests to Exchange 2003 Server. It is set to be fixed in SP1 RU7 and SP2 RU1.  To request an interim update please contact Microsoft.";
			}
		}
		return flag;
	}

	public string[] DumpArrayElements(object arrayAddress, string fieldName, object className)
	{
		dynamic objectAt = GetObjectAt(arrayAddress);
		if (objectAt == null)
		{
			return new string[0];
		}
		List<string> list = new List<string>();
		for (int i = 0; i < objectAt.GetLength(); i++)
		{
			dynamic val = objectAt[i];
			if (val != null && !(val is ClrNullValue))
			{
				ClrInstanceField fieldByName = ((ClrType)val.GetHeapType()).GetFieldByName(fieldName);
				ulong num = fieldByName.GetFieldAddress(val.GetValue(), false);
				if (Globals.g_Debugger.ClrRuntime.ReadPointer(num, ref num))
				{
					list.Add(num.ToString("x"));
				}
			}
		}
		return list.ToArray();
	}

	public void PrintDebugModuleInformation()
	{
		string text = "";
		object obj = null;
		object obj2 = 0;
		object obj3 = 0;
		object obj4 = 0;
		object obj5 = 0;
		int num = 0;
		foreach (ClrModule item in Globals.g_Debugger.ClrRuntime.EnumerateModules())
		{
			if (item.DebuggingMode.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations) && (item.DebuggingMode - 256).CompareTo(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints) > 0)
			{
				IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(item.ImageBase);
				if (moduleByAddress != null && moduleByAddress.VSCompanyName.IndexOf("Microsoft Corporation") == -1)
				{
					num++;
					moduleByAddress.GetFileVersion(ref obj2, ref obj3, ref obj4, ref obj5);
					obj = regexReplace(moduleByAddress.TimeStamp, "(([0-1][0-9])|([2][0-3])):([0-5][0-9]):([0-5][0-9])|^(Sun|Mon|Tue|Thu|Fri|Sat|Wed)", "");
					text = text + "<tr><td align='left' title='" + Convert.ToString(moduleByAddress.ImageName) + "\n'>" + moduleByAddress.ModuleName + "</td><td align='right'> " + Convert.ToString(obj2) + "." + Convert.ToString(obj3) + "." + Convert.ToString(obj4) + "." + Convert.ToString(obj5) + "</td><td align='right'>" + Convert.ToString(obj) + "</td><td align='right'>" + Convert.ToString(moduleByAddress.VSCompanyName) + "</td></tr>";
				}
			}
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("RuntimesDebugTrue", (SectionType)0);
		val.Collapsible = true;
		val.Title = "List of modules compiled in Debug mode";
		val.Write("<PRE>");
		if (text != "")
		{
			val.Write("<table border=0 cellpadding=1 cellspacing=5 class=myCustomText><tr><td><b>Module</b></td><td><b>Version</b></td><td><b>Time</b></td><td><b>Company</b></td></tr>");
			val.Write(text);
			val.Write("</table>");
		}
		else
		{
			val.Write("<b>No Modules compiled in Debug mode</b>");
		}
		val.Write("</PRE>");
	}

	public void CheckBufferedSystemDotNetConnections()
	{
		string[] array = null;
		int num = 0;
		int num2 = 0;
		object obj = null;
		object obj2 = null;
		object[] array2 = null;
		string text = "";
		object obj3 = null;
		object obj4 = null;
		array = DumpHeapForType("System.Net.ConnectionGroup");
		if (Globals.HelperFunctions.IsNullOrEmpty(array))
		{
			return;
		}
		for (num = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array, 1); num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num++)
		{
			obj = DumpObject(array[num], "m_ConnectionList");
			obj2 = DumpObject(obj, "_items");
			array2 = DumpObjectArrayRaw(obj2);
			if (Globals.HelperFunctions.IsNullOrEmpty(array2) || array2 == null)
			{
				continue;
			}
			for (num2 = Globals.HelperFunctions.LBound_HACK_DO_NOT_USE(array2, 1); num2 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1); num2++)
			{
				if (array2[num2].ToString() != "0")
				{
					obj4 = DumpObject(array2[num2], "m_ResponseData");
					int num3 = (int)DumpLong(obj4, "m_StatusCode");
					if (Information.IsNumeric(num3) && num3 > 200)
					{
						text = "";
						obj3 = DumpObject(array2[num2], "m_ReadBuffer");
						text = ((Convert.ToInt32(Globals.g_SizeOfULongPtr) != 8) ? Convert.ToString(Globals.g_Debugger.ReadANSIString(Globals.HelperFunctions.FromHex((string)obj3)) + 8) : Convert.ToString(Globals.g_Debugger.ReadANSIString(Globals.HelperFunctions.FromHex((string)obj3)) + 16));
						Globals.g_SysNetConnectionWithBuffer.Add(Globals.HelperFunctions.FromHex((string)array2[num2]), text);
					}
				}
			}
		}
	}

	public void CheckLeakedSystemDotNetConnections()
	{
		object obj = null;
		object obj2 = null;
		foreach (string key in Globals.g_SysNetServicePointsWaitingOnSocket.Keys)
		{
			obj2 = DumpLong(key, "m_ConnectionLimit");
			if (obj2 != Globals.NOT_FOUND && Convert.ToInt32(Globals.g_SysNetServicePointsWaitingOnSocket[key]) < Convert.ToInt32(obj2))
			{
				Globals.Manager.ReportError("The connection limit (" + Convert.ToString(obj2) + ") for the <b>System.Net.ServicePoint</b> is hit but there are only " + Convert.ToString(Globals.g_SysNetServicePointsWaitingOnSocket[key]) + " threads who are actually making the socket calls", "This is an indication that System.Net.Connections are getting leaked. You must call either the Stream.Close or the HttpWebResponse.Close method to close the stream and release the connection for reuse. It is not necessary to call both Stream.Close and HttpWebResponse.Close, but doing so does not cause an error. Failure to close the stream can cause your application to run out of connections <br/><br/> For more details refer to this article <a href='http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.close.aspx'>http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.close.aspx</a>", 0, "{dce7555f-cf36-426e-9576-e0551a3635a4}");
			}
		}
	}

	public string GetOwnerIdForHttpApplicationStateLock(CacheFunctions.ScriptThreadClass Thread)
	{
		string text = "";
		object obj = null;
		object obj2 = null;
		obj = FindObjectFromDSO("HttpApplicationStateLock", Thread.ThreadID, true);
		if (!Globals.HelperFunctions.IsNullOrEmpty(obj))
		{
			obj2 = DumpLong(obj, "_threadId");
			if (obj2 != Globals.NOT_FOUND)
			{
				return Convert.ToString(obj2);
			}
			return "-1";
		}
		return "-1";
	}

	public bool CheckInClrStackIfSleepIsCozMSFT(CacheFunctions.ScriptThreadClass Thread)
	{
		bool flag = false;
		int num = 0;
		string text = "";
		string text2 = "";
		flag = false;
		if (Thread.FindFrameInClrStack("SYSTEM.THREADING.THREAD.SLEEPINTERNAL") > -1)
		{
			num = Convert.ToInt32(Thread.FindFrameInClrStack("SYSTEM.THREADING.THREAD.SLEEPINTERNAL")) + 1;
			string text3 = Convert.ToString("#FRAMENUM#") + Convert.ToString(num) + Convert.ToString("#");
			text = text3 + "MICROSOFT";
			text2 = text3 + "SYSTEM";
			flag = Convert.ToString(Thread.m_ClrSearchString).IndexOf(text) >= 0 || Convert.ToString(Thread.m_ClrSearchString).IndexOf(text2) >= 0;
		}
		return flag;
	}

	public string GetManagedExceptionPtr(CacheFunctions.ScriptThreadClass ExceptionThread)
	{
		if (ExceptionThread == null)
		{
			throw new ArgumentNullException("ExceptionThread");
		}
		if (ExceptionThread.m_dbgThread == null || ExceptionThread.m_dbgThread.ManagedThread == null)
		{
			return null;
		}
		ClrException currentException = ExceptionThread.m_dbgThread.ManagedThread.CurrentException;
		if (currentException == null)
		{
			return null;
		}
		return currentException.Address.ToString("x");
	}
}
