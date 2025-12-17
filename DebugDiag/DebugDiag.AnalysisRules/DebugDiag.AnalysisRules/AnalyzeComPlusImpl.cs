using System;
using System.Collections.Generic;
using ComplusDDExt;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

public class AnalyzeComPlusImpl : IAnalyzeComPlus
{
	public const int INCALLCOUNT_IN_CALL_ASP = -4;

	public const int INCALLCOUNT_NO_APPINVOKE_BUT_YES_DOWORK = -3;

	public const int INCALLCOUNT_UNKNOWN = -2;

	public const int INCALLCOUNT_UNKNOWN_LIKELY_IN_CALL = -1;

	public const int INCALLCOUNT_IDLE = 0;

	public const int INCALLCOUNT_IN_CALL = 1;

	public const string INCALLSTATUS_IN_CALL_ASP = "<font color=navy>In-Call (ASP)</font>";

	public const string INCALLSTATUS_NO_APPINVOKE_BUT_YES_DOWORK = "<font color=navy>In-Call (custom)</font>";

	public const string INCALLSTATUS_UNKNOWN = "<font color=red>Unknown</font>";

	public const string INCALLSTATUS_UNKNOWN_LIKELY_IN_CALL = "<font color=navy>In-Call (bad symbols)</font>";

	public const string INCALLSTATUS_IDLE = "<font color=green>Idle</font>";

	public const string INCALLSTATUS_IN_CALL = "<font color=navy>In-Call</font>";

	public const string INCALLSTATUS_IN_CALL_STACKED_1 = "<font color=navy>In-Call</font> <font color=red>(";

	public const string INCALLSTATUS_IN_CALL_STACKED_2 = " calls stacked)</font>";

	public void AnalyzeComAndComPlusInfo()
	{
		Globals.g_collDeadLockedActivites.Clear();
		Globals.g_collEnteredButNonDeadlockedActivites.Clear();
		Globals.g_collTransitioningActivites.Clear();
		Globals.g_collCOMPlusSTAThreadPoolThreads.Clear();
		Globals.g_collSpecialSTAThreads.Clear();
		Globals.g_currentStaThreadCount = 0;
		if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule)))
		{
			BuildSpecialSTAThreadsCache();
		}
		if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("COMSVCS")))
		{
			if (Globals.g_OSVER == Globals.OS_VER_UNKNOWN)
			{
				Globals.Manager.WriteLine("WARNING - Skipping COM/COM+ analysis due to unknown OS version in target dump.");
				return;
			}
			Globals.HelperFunctions.ResetStatus("Cataloging all COM+ activity lock data (loading symbol files may take a moment)", 5, "Step");
			Globals.g_ComplusExt = (IComplusRoot)Globals.g_Debugger.GetExtensionObject("COMPlusDDExt", "CComplusRoot");
			Globals.HelperFunctions.IncrementSubStatus();
			Globals.AnalyzeActivities.SortAllActivities();
			Globals.HelperFunctions.IncrementSubStatus();
			CollectSTAThreadPoolData();
			Globals.HelperFunctions.IncrementSubStatus();
			Globals.AnalyzeActivities.ReportAllActivities();
			Globals.HelperFunctions.IncrementSubStatus();
			SaveThreadPoolProblemsForLater();
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	private void BuildSpecialSTAThreadsCache()
	{
		Globals.HelperFunctions.ResetStatus("Cataloging special COM STA threads...", 6, "Special STA Thread");
		Globals.g_collSpecialSTAThreads = new Dictionary<double, double>();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetMainSTAApartmentID()));
		Globals.HelperFunctions.IncrementSubStatus();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetHostApartmentID("ST")));
		Globals.HelperFunctions.IncrementSubStatus();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetHostApartmentID("STMT")));
		Globals.HelperFunctions.IncrementSubStatus();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetHostApartmentID("AT")));
		Globals.HelperFunctions.IncrementSubStatus();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetHostApartmentID("MT")));
		Globals.HelperFunctions.IncrementSubStatus();
		AddToSpecialSTAThreadsCache(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(GetHostApartmentID("NT")));
		Globals.HelperFunctions.IncrementSubStatus();
	}

	private void AddToSpecialSTAThreadsCache(double nLogicalTID)
	{
		if (!Globals.g_collSpecialSTAThreads.ContainsKey(nLogicalTID))
		{
			Globals.g_collSpecialSTAThreads.Add(nLogicalTID, nLogicalTID);
		}
	}

	public string GetSTAThreadPoolReportLink()
	{
		return "<a href='#STAThreadPoolReport" + Globals.g_UniqueReference + "'><b>COM+ STA ThreadPool Report</b></a>";
	}

	public string GetCOMPlusComponentStatsLink()
	{
		return "<a href='#COMPlusComponentStats" + Globals.g_UniqueReference + "'><b>COM+ Component Statistics Report</b></a>";
	}

	public string GetCOMSTAReportLink()
	{
		return "<a href='#COMSTAReport" + Globals.g_UniqueReference + "'><b>Well-Known COM STA Threads Report</b></a>";
	}

	private void GetComponentStats()
	{
		if (Convert.ToInt64(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_pAppTrackerObject")) != 0L)
		{
			if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
			{
				Globals.g_instanceCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("poi(comsvcs!g_pAppTrackerObject)+1C"));
				Globals.g_inCallCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("poi(comsvcs!g_pAppTrackerObject)+20"));
			}
			else
			{
				Globals.g_instanceCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("poi(comsvcs!g_pAppTrackerObject)+10"));
				Globals.g_inCallCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("poi(comsvcs!g_pAppTrackerObject)+14"));
			}
		}
		else
		{
			Globals.g_instanceCount = -1;
			Globals.g_inCallCount = -1;
		}
	}

	private void GetSTAPoolStats()
	{
		if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
		{
			Globals.g_currentStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+8"));
			Globals.g_maxStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+C"));
			Globals.g_minStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+10"));
			Globals.g_activitiesPerThread = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+14"));
			Globals.g_emulateMTS = Globals.g_activitiesPerThread == 1 && Globals.g_maxStaThreadCount == 100;
		}
		else if (Globals.g_OSVER == Globals.OS_VER_WIN2K)
		{
			Globals.g_currentStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!STAThread::sm_cThreads"));
			Globals.g_maxStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!STAThread::sm_cMaxThreads"));
			Globals.g_minStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!STAThread::sm_cMinThreads"));
			Globals.g_activitiesPerThread = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!STAThread::sm_cActivitiesPerThread"));
			Globals.g_emulateMTS = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!STAThread::sm_EmulateMTS")) > 0;
		}
		else if (Globals.g_OSVER >= Globals.OS_VER_WINXP)
		{
			Globals.g_currentStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+4"));
			Globals.g_maxStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+8"));
			Globals.g_minStaThreadCount = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+c"));
			Globals.g_activitiesPerThread = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolAndReportErrors("comsvcs!g_STAThreadPool+10"));
			Globals.g_emulateMTS = Convert.ToInt32(Globals.g_activitiesPerThread) == 1 && Convert.ToInt32(Globals.g_maxStaThreadCount) == 100;
		}
	}

	private void GetSTAThreadInfo()
	{
		Globals.g_staThreadsInCall = 0;
		string[] sTAThreadPoolThreadArray = GetSTAThreadPoolThreadArray();
		if (sTAThreadPoolThreadArray == null)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Collecting COM+ STA ThreadPool data", sTAThreadPoolThreadArray.Length / 4, "STA Thread");
		for (int i = 1; i < sTAThreadPoolThreadArray.Length; i += 4)
		{
			int num = Convert.ToInt32(Globals.HelperFunctions.FromHex(sTAThreadPoolThreadArray[i]));
			int value = Convert.ToInt32(Globals.HelperFunctions.FromHex(sTAThreadPoolThreadArray[i + 2]));
			int num2 = STAThreadInCallCount(num);
			Globals.g_colSTAThreadActivityCounts.Add(num, value);
			Globals.g_colSTAThreadInCallStates.Add(num, num2);
			if (Convert.ToInt32(value) > 0)
			{
				Globals.g_boundThreadCount++;
			}
			if (Convert.ToInt32(value) > 1)
			{
				Globals.g_activityPileUpCount++;
			}
			if (num2 != -2 && num2 != 0)
			{
				Globals.g_staThreadsInCall++;
			}
			double num3 = Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(num);
			if (Convert.ToInt32(num3) >= 0 && !Globals.g_collCOMPlusSTAThreadPoolThreads.ContainsKey(num3))
			{
				Globals.g_collCOMPlusSTAThreadPoolThreads.Add(num3, num3);
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	private string GetSTATIDAndActivityCountCmd()
	{
		string result = "";
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = null;
		bool flag = false;
		CacheFunctions.ScriptModuleClass moduleByName = Globals.g_ModuleCache.GetModuleByName("COMSVCS");
		moduleByName.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
		text5 = moduleByName.VSFileVersion;
		if (Globals.g_OSVER == Globals.OS_VER_WIN2K)
		{
			Globals.g_ModuleCache.GetModuleByName("COMSVCS").GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
			if (Convert.ToInt32(Build) < 3503)
			{
				text = "10";
				text2 = "14";
			}
			else
			{
				text = "c";
				text2 = "10";
			}
			if (Convert.ToInt32(Globals.g_Debugger.ReadDWord(Convert.ToDouble(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!STAThread::sm_threads")))) == 0)
			{
				return result;
			}
			result = "r $t1=0; .foreach ( STAThread { dl poi(comsvcs!STAThread::sm_threads) poi(comsvcs!STAThread::sm_cThreads) 1 } ) { r $t1 = @$t1 + 1; .if (@$t1=1) {dd ( STAThread +" + text + ") l1; dd ( STAThread +" + text2 + ") l1 } .elsif (@$t1=2) {r $t1=0} } ";
		}
		else if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
		{
			text3 = "B0";
			text4 = "B8";
			text = "28";
			text2 = "30";
			result = "r $t0=3; .foreach (STAThread {dq poi(comsvcs!g_STAThreadPool+" + text3 + ") poi(comsvcs!g_STAThreadPool+" + text4 + ")-8} ) {.if (@$t0!=3) {dd ( STAThread +" + text + ") l1; dd ( STAThread +" + text2 + ") l1; r $t0= @$t0+1} .else {r $t0=1} }";
		}
		else if (Globals.g_OSVER >= Globals.OS_VER_WIN2K3)
		{
			if (Globals.g_OSVER == Globals.OS_VER_WIN2K3 || Globals.g_OSVER == Globals.OS_VER_WIN2K3SP)
			{
				if (Convert.ToString(text5).ToLower().IndexOf("srv03_qfe") >= 0)
				{
					if (Convert.ToInt32(Priv) < 307)
					{
						flag = true;
					}
				}
				else if (Convert.ToString(text5).ToLower().IndexOf("srv03_sp1_qfe") >= 0 && Convert.ToInt32(Priv) < 2419)
				{
					flag = true;
				}
			}
			if (flag)
			{
				text3 = "88";
				text4 = "8c";
			}
			else
			{
				text3 = "8c";
				text4 = "90";
			}
			text = "14";
			text2 = "1c";
			result = "r $t0=5; .foreach (STAThread {dd poi(comsvcs!g_STAThreadPool+" + text3 + ") poi(comsvcs!g_STAThreadPool+" + text4 + ")-4} ) {.if (@$t0!=5) {dd ( STAThread +" + text + ") l1; dd ( STAThread +" + text2 + ") l1; r $t0= @$t0+1} .else {r $t0=1} }";
		}
		Convert.ToInt32(Globals.g_Debugger.ReadDWord(Convert.ToDouble(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!g_STAThreadPool+" + text3))));
		return result;
	}

	private void CollectSTAThreadPoolData()
	{
		Globals.g_colSTAThreadInCallStates = new Dictionary<int, int>();
		Globals.g_colSTAThreadActivityCounts = new Dictionary<int, int>();
		GetSTAPoolStats();
		GetSTAThreadInfo();
		GetComponentStats();
	}

	private string GetInCallStatutsString(int nInCallCount)
	{
		string text = "";
		switch (nInCallCount)
		{
		case -4:
			return Convert.ToString("<font color=navy>In-Call (ASP)</font>");
		case -3:
			return Convert.ToString("<font color=navy>In-Call (custom)</font>");
		case -2:
			return Convert.ToString("<font color=red>Unknown</font>");
		case -1:
			return Convert.ToString("<font color=navy>In-Call (bad symbols)</font>");
		case 0:
			return Convert.ToString("<font color=green>Idle</font>");
		case 1:
			return Convert.ToString("<font color=navy>In-Call</font>");
		default:
			if (nInCallCount > 1)
			{
				return Convert.ToString("<font color=navy>In-Call</font> <font color=red>(") + Convert.ToString(nInCallCount) + Convert.ToString(" calls stacked)</font>");
			}
			return Convert.ToString("<font color=red>Unknown</font>");
		}
	}

	private int STAThreadInCallCount(int staTID)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		int num = 0;
		int result = 0;
		scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(staTID);
		if (scriptThreadClass == null)
		{
			return result;
		}
		if (Globals.g_ASPInfo.GetASPRequestByThreadID(scriptThreadClass.ThreadID) != null)
		{
			return -4;
		}
		if (Globals.g_OSVER <= Globals.OS_VER_WIN2K)
		{
			num = 7;
		}
		else if (Globals.g_OSVER == Globals.OS_VER_WIN2K3 || Globals.g_OSVER == Globals.OS_VER_WIN2K3SP || Globals.g_OSVER == Globals.OS_VER_WINXP)
		{
			num = ((!(Convert.ToString(Globals.g_OSPlatformVersion) == "X64")) ? 8 : 7);
		}
		else if (Globals.g_OSVER == Globals.OS_VER_WINVISTA || Globals.g_OSVER == Globals.OS_VER_WIN7)
		{
			num = ((!Convert.ToBoolean(IsHostCOMSTA(staTID))) ? 11 : ((Globals.g_OSPlatformVersion == "X64") ? 8 : 10));
		}
		else if (Globals.g_OSVER >= Globals.OS_VER_WIN8)
		{
			num = ((!Convert.ToBoolean(IsHostCOMSTA(staTID))) ? ((Globals.g_OSPlatformVersion == "X64") ? 8 : 11) : ((Globals.g_OSPlatformVersion == "X64") ? 8 : 10));
		}
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = scriptThreadClass.StackFrames;
		if (Convert.ToInt32(stackFrames.Count) < num)
		{
			result = -2;
		}
		else
		{
			result = Globals.AnalyzeComPlus.CountAppInvokeFrames(scriptThreadClass, 0);
			if (result == 0)
			{
				if (Convert.ToInt32(stackFrames.Count) > num && !scriptThreadClass.HasAllGoodSymbols)
				{
					result = -1;
				}
			}
			else if (result > 1)
			{
				Globals.g_COMPlusSTAStackingPresent = true;
			}
		}
		return result;
	}

	public int CountAppInvokeFrames(CacheFunctions.ScriptThreadClass Thread, int nStopAt)
	{
		int num = 0;
		num = Thread.CountFrameHitsInStack(Globals.g_COMRuntimeModule + "!APPINVOKE", nStopAt);
		if ((double)num == 0.0)
		{
			if (Thread.FindFrameInStack("ASP!CVIPERASYNCREQUEST::ONCALL") > -1)
			{
				num = -4;
			}
			else if (Thread.FindFrameFragmentsInStack(new string[2] { "COMSVCS!", "::DOWORK" }) > -1)
			{
				num = -3;
			}
		}
		else if ((double)num > 0.0)
		{
			num -= Thread.CountFrameHitsInStack(Globals.g_COMRuntimeModule + "!APPINVOKEEXCEPTIONFILTER", nStopAt);
		}
		return num;
	}

	private string GetComPlusSTAThreadPoolProblemRecommendation(double percentThreadsBusy, object emulateMTSBehaviorIsSet, object g_instanceCount)
	{
		string text = "";
		if (percentThreadsBusy >= 0.5)
		{
			text = "<b>" + Convert.ToString(Globals.HelperFunctions.GetPercentageString(percentThreadsBusy, bBold: true, bCapital: true)) + "</b> of the STA ThreadPool threads are <b>busy</b> processing calls.  ";
			if (Convert.ToBoolean(Globals.g_IsBlockingIssueDetected))
			{
				text += "DebugDiag has detected one or more blocking issues in this dump, so the STA ThreadPool problem may simply be a symptom of another problem.  Review the other Errors and Warnings in the Analysis Summary section of the report for the details on the blocking issue(s).";
			}
			else
			{
				text = text + "DebugDiag did not detect any blocking issues in this dump, so manually inspect the in-call COM+ STA ThreadPool threads listed in the " + GetSTAThreadPoolReportLink() + " to determine what they are blocking on, and address the root cause of the blocking.";
				if (!Convert.ToBoolean(emulateMTSBehaviorIsSet))
				{
					text += "<br><br><br><font color='Gray'>Note:  In rare cases the STA threads may be blocking on an operation which is <i>expected</i> to take a long time (i.e. a synchronous database call).  In such cases, if CPU utilization is low, you can consider tweaking the ";
					text = ((!Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("ASP"))) ? (text + "STAThreadPool registry settings to accommodate more concurrent requests. For more information see the following Knowledge Base article:  <a target='_blank' href='http://support.microsoft.com/?id=303071'>303071 Registry key for tuning COM+ thread and activity</a></font></LI>") : (text + "AspProcessorThreadMax metabase setting to accommodate more concurrent requests. For more information see the following Knowledge Base articles: <br>" + Globals.HelperFunctions.Spaces(5) + "<a target='_blank' href='http://support.microsoft.com/?id=253146'>253146 Tuning the Performance and Scalability of ASP Web Applications</a><br>" + Globals.HelperFunctions.Spaces(5) + "<a target='_blank' href='http://support.microsoft.com/?id=238583'>238583 How to Tune the ASPProcessorThreadMax</a></font></LI>"));
				}
			}
		}
		else
		{
			text = "<b>" + Convert.ToString(Globals.HelperFunctions.GetPercentageString(1.0 - percentThreadsBusy, bBold: true, bCapital: true)) + "</b> of the STA ThreadPool threads are <b>idle</b>, however some threads still have more than one activity bound to them.  This can happen for various reasons:<UL><LI>If COM+ objects are being leaked.  ";
			if (Convert.ToInt32(g_instanceCount) > 0)
			{
				text = text + "This may correspond with a high \"Component Instances\" count.  In this dump there are <b>" + Convert.ToString(g_instanceCount) + "</b> instances.";
			}
			text += "If there is a memory leak, <b>you should use DebugDiag to monitor the leak for root cause</b>.  See the \"Troubleshooting Memory Leaks with DebugDiag\" topic in the DebugDiag.chm file for more information.</LI><LI>If many clients are holding onto object references but the objects are not currently in-call.  In such a case you should consider changing the client code to release the object references as quickly as possible, since concurrent requests for different objects that are bound to the same STA ThreadPool thread will be serialized (even if other STA ThreadPool threads are idle).</LI></UL>";
			text += "<br><br><br><font color='Gray'>Note:  When calling COM or COM+ components from ASP pages, one COM+ activity will remain behind for each active IIS sesion.  In such a case, the activities are expected and do not indicate a problem.  ";
			text = ((!Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("ASP"))) ? (text + "The ASP runtime is not loaded in this process, but to completely rule out the possibility of this scenario you should confirm that the COM+ components in this application are not called from ASP pages in another process.</font>") : (text + "The ASP runtime is loaded in this process, which increases the likelihood of this scenario.</font>"));
		}
		return text;
	}

	private void SaveThreadPoolProblemsForLater()
	{
		bool flag = false;
		string text = "";
		bool flag2 = false;
		if (Convert.ToInt32(Globals.g_currentStaThreadCount) <= 0)
		{
			return;
		}
		if (Convert.ToInt32(Globals.g_activityPileUpCount) > 0)
		{
			if (Globals.g_activityPileUpCount == Globals.g_currentStaThreadCount)
			{
				text = "A COM+ STA Activity Pileup has been detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>.  There is more than one activity bound to every COM+ STA ThreadPool thread.";
				flag = false;
				flag2 = true;
			}
			else if (Globals.g_boundThreadCount == Globals.g_currentStaThreadCount)
			{
				text = "A COM+ STA Activity Pileup has been detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>.  There is at least one activity bound to every COM+ STA ThreadPool thread, with the following threads having more than one activity bound:<br><br>";
				flag = true;
				flag2 = true;
			}
			else
			{
				text = "The COM+ STA ThreadPool may have been depleted prior to the time of the dump in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>.  There is more than one activity bound to the following COM+ STA ThreadPool threads:<br><br>";
				flag = true;
				flag2 = false;
			}
		}
		else if (Globals.g_maxStaThreadCount == Globals.g_currentStaThreadCount)
		{
			if (Globals.g_boundThreadCount == Globals.g_currentStaThreadCount)
			{
				text = "A COM+ STA Activity Pileup has been detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>.  The pool has grown to its maximum allowable size and each thread has one activity bound to it.";
				flag2 = true;
			}
			else
			{
				text = "The COM+ STA ThreadPool may have been depleted prior to the time of the dump in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>.  The pool has grown to its maximum allowable size, but some threads do not currently have any activity bound to it.";
				flag2 = false;
			}
		}
		if (!(text != ""))
		{
			return;
		}
		if (flag)
		{
			foreach (KeyValuePair<int, int> g_colSTAThreadActivityCount in Globals.g_colSTAThreadActivityCounts)
			{
				if (g_colSTAThreadActivityCount.Value > 1)
				{
					text = text + Globals.HelperFunctions.GetThreadIDWithLinkFromDecSystemID(g_colSTAThreadActivityCount.Key) + "  ";
				}
			}
		}
		text = text + "<br><br>See the " + GetSTAThreadPoolReportLink() + " for more detail.";
		Globals.g_COMPlusReport = new COMPlusReportClass();
		Globals.g_COMPlusReport.Description = text;
		Globals.g_COMPlusReport.EmulateMTSBehaviorIsSet = Globals.g_emulateMTS;
		Globals.g_COMPlusReport.InstanceCount = Globals.g_instanceCount;
		if (flag2)
		{
			Globals.g_COMPlusReport.IsError = true;
			Globals.g_IsComPlusSTAPoolIssueDetected = true;
		}
		else
		{
			Globals.g_COMPlusReport.IsWarning = true;
		}
	}

	public void ReportCOMPlusInfo()
	{
		string text = "";
		object obj = null;
		string text2 = "";
		string text3 = null;
		string text4 = "";
		if (Globals.g_currentStaThreadCount > 0)
		{
			if (Globals.g_COMPlusReport != null && Convert.ToBoolean(~Convert.ToInt32(Globals.g_IsBlockingIssueDetected)) && Globals.g_COMPlusReport != null)
			{
				text = GetComPlusSTAThreadPoolProblemRecommendation(Convert.ToDouble(Globals.g_staThreadsInCall) / Convert.ToDouble(Globals.g_currentStaThreadCount), Globals.g_emulateMTS, Globals.g_instanceCount);
				if (Convert.ToBoolean(Globals.g_COMPlusReport.IsError))
				{
					Globals.Manager.ReportError(Globals.g_COMPlusReport.Description, text, Globals.g_COMPlusReport.Weight, "{b6acb670-ab7d-4f80-b0bf-78f5c313a9d4}");
				}
				else if (Convert.ToBoolean(Globals.g_COMPlusReport.IsWarning))
				{
					Globals.Manager.ReportWarning(Globals.g_COMPlusReport.Description, text, Globals.g_COMPlusReport.Weight, "{4f398951-d0c4-4315-9eaa-db627e694bab}");
				}
				else if (Convert.ToString(Globals.g_COMPlusReport.Description) != "")
				{
					Globals.Manager.ReportInformation(Globals.g_COMPlusReport.Description, 0, "{d9453c01-297d-4f19-a4ac-0c42a00696e3}");
				}
			}
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("STAThreadPoolReport", (SectionType)0);
			val.Title = "COM+ STA ThreadPool Report";
			val.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><td><b>Max STA Threads</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_maxStaThreadCount) + "</td></tr><tr><td><b>Min STA Threads</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_minStaThreadCount) + "</td></tr><tr><td><b>Current STA Threads</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_currentStaThreadCount) + "</td></tr><tr><td><b>g_activitiesPerThread</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_activitiesPerThread) + "</td></tr><tr><td><b>EmulateMTSBehavior</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_emulateMTS) + "</td></tr><tr><td><b>STA Threads In-Call</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_staThreadsInCall) + Globals.HelperFunctions.Spaces(2) + "(" + Convert.ToString(Globals.HelperFunctions.GetPercentageString(Convert.ToDouble(Globals.g_staThreadsInCall) / Convert.ToDouble(Globals.g_currentStaThreadCount), bBold: false, bCapital: false)) + ")</td></tr></table><br>");
			if (Globals.g_collCOMPlusSTAThreadPoolThreads.Count == 0)
			{
				val.Write("<b>Note:</b>  There was an error collecting details for the COM+ STA Threadpool threads.  The internal COM+ data structures may have been corrupted.<br>");
			}
			else
			{
				text2 = ((!Globals.g_COMPlusSTAStackingPresent) ? "Call Status" : "Call Status </b>(top-most call for stacked threads)<b>");
				val.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><th align=left><u>STA Thread</u>" + Globals.HelperFunctions.Spaces(7) + "</th><th align=left><u>Activity Count</u>" + Globals.HelperFunctions.Spaces(7) + "</th><th align=left><u>Thread Status</u>" + Globals.HelperFunctions.Spaces(7) + "</th><th align=left><u>" + text2.ToString() + "</u></th></tr>");
				Globals.HelperFunctions.ResetStatus("Building COM+ STA ThreadPool report", Globals.g_collDeadLockedActivites.Count, "STA Thread");
				foreach (KeyValuePair<int, int> g_colSTAThreadActivityCount in Globals.g_colSTAThreadActivityCounts)
				{
					obj = g_colSTAThreadActivityCount.Value;
					text3 = GetInCallStatutsString(Globals.g_colSTAThreadInCallStates[g_colSTAThreadActivityCount.Key]);
					text4 = ((!(text3 == "<font color=green>Idle</font>")) ? Convert.ToString(Globals.HelperFunctions.GetTrimmedThreadDescription(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(g_colSTAThreadActivityCount.Key))) : "(N/A)");
					val.Write("<tr><td align=left>" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLinkFromDecSystemID(g_colSTAThreadActivityCount.Key)) + "</td><td align=left>" + Convert.ToString(obj) + "</td></td><td align=left>" + Convert.ToString(text3) + Globals.HelperFunctions.Spaces(3) + "</td></td><td align=left>" + text4 + "</td></tr>");
					Globals.HelperFunctions.IncrementSubStatus();
				}
				val.Write("</table><br>");
			}
		}
		if (Convert.ToInt32(Globals.g_instanceCount) > 0)
		{
			ReportSection obj2 = Globals.Manager.CurrentSection.AddChildSection("COMPlusComponentStats", (SectionType)0);
			obj2.Title = "COM+ Component Statistics Report";
			obj2.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><td><b>Component Instances</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_instanceCount) + "</td></tr><tr><td><b>Components InCall</b>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>" + Convert.ToString(Globals.g_inCallCount) + "</td></tr></table><br>");
		}
	}

	private void GetThreadNumAndInCallStatusStringBySystemTID(int systemTID, ref int nThreadNum, ref string sInCallStatus, ref bool bThreadIsStacked)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		if (Convert.ToInt32(systemTID) == 0)
		{
			nThreadNum = -1;
			return;
		}
		scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(systemTID);
		if (scriptThreadClass == null)
		{
			nThreadNum = -1;
			return;
		}
		int num = STAThreadInCallCount(systemTID);
		sInCallStatus = GetInCallStatutsString(num);
		nThreadNum = scriptThreadClass.ThreadID;
		scriptThreadClass = null;
		if (num > 1)
		{
			bThreadIsStacked = true;
		}
	}

	public void ReportCOMInfo()
	{
		string text = "";
		Dictionary<string, string> dictionary = null;
		string text2 = "";
		int num = 0;
		string text3 = null;
		string text4 = "";
		bool bThreadIsStacked = false;
		int[] array = new int[4];
		string[] array2 = new string[4];
		string[] array3 = new string[4];
		if (!Convert.ToBoolean(IsAnyWellKnownCOMSTALoaded()))
		{
			return;
		}
		dictionary = new Dictionary<string, string>();
		text2 = "MAIN";
		dictionary.Add(text2, "Main STA");
		array3[num] = text2;
		num++;
		text2 = "ST";
		dictionary.Add(text2, "Single-threaded host for STA clients");
		array3[num] = text2;
		num++;
		text2 = "STMT";
		dictionary.Add(text2, "Single-threaded host for MTA clients");
		array3[num] = text2;
		num++;
		text2 = "AT";
		dictionary.Add(text2, "Apartment-threaded host for MTA clients");
		array3[num] = text2;
		num++;
		for (num = 0; num < array3.Length; num++)
		{
			GetThreadNumAndInCallStatusStringBySystemTID(GetHostApartmentID(array3[num]), ref array[num], ref array2[num], ref bThreadIsStacked);
			Globals.g_COMSTAStackingPresent |= bThreadIsStacked;
		}
		text = ((!Globals.g_COMSTAStackingPresent) ? "Call Status" : "Call Status </b>(top-most call for stacked threads)<b>");
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("COMSTAReport", (SectionType)0);
		val.Title = "Well-Known COM STA Threads Report";
		val.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><th align=left><u>STA Name</u>" + Convert.ToString(Globals.HelperFunctions.Spaces(3)) + "</th><th align=left><u>Thread ID</u>" + Convert.ToString(Globals.HelperFunctions.Spaces(3)) + "</th><th align=left><u>Thread Status</u>" + Convert.ToString(Globals.HelperFunctions.Spaces(3)) + "</th><th align=left><u>" + text + "</u></th></tr>");
		for (num = 0; num < array3.Length; num++)
		{
			if (array[num] > -1)
			{
				text3 = array2[num];
				text4 = ((!(text3 == "<font color=green>Idle</font>")) ? Convert.ToString(Globals.HelperFunctions.GetTrimmedThreadDescription(array[num])) : "(N/A)");
				val.Write("<tr><td align=left>" + Convert.ToString(dictionary[array3[num]]) + Convert.ToString(Globals.HelperFunctions.Spaces(3)) + "</td><td align=left>" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(array[num])) + "</td><td align=left>" + Convert.ToString(text3) + Globals.HelperFunctions.Spaces(3) + "</td></td><td align=left>" + text4 + "</td></tr>");
			}
		}
		val.Write("</table><br>");
	}

	public bool IsAnyWellKnownCOMSTALoaded()
	{
		bool flag = false;
		flag = false;
		if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule)))
		{
			flag = true;
			if (Convert.ToInt32(GetMainSTAApartmentID()) == 0 && GetHostApartmentID("ST") == 0 && GetHostApartmentID("STMT") == 0 && GetHostApartmentID("AT") == 0)
			{
				flag = false;
			}
		}
		return flag;
	}

	public bool IsWellKnownCOMSTA(int systemTID)
	{
		bool flag = false;
		flag = false;
		if (Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule) && Convert.ToInt32(systemTID) > 0)
		{
			flag = true;
			if (GetMainSTAApartmentID() != systemTID && !IsHostCOMSTA(systemTID))
			{
				flag = false;
			}
		}
		return flag;
	}

	private bool IsHostCOMSTA(object systemTID)
	{
		bool flag = false;
		flag = false;
		if (Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule) && Convert.ToInt32(systemTID) > 0)
		{
			flag = true;
			if (GetHostApartmentID("ST") != Convert.ToInt32(systemTID) && GetHostApartmentID("STMT") != Convert.ToInt32(systemTID) && GetHostApartmentID("AT") != Convert.ToInt32(systemTID))
			{
				flag = false;
			}
		}
		return flag;
	}

	public int GetCurrentSTAThreadCount()
	{
		int num = 0;
		num = 0;
		if (Globals.HelperFunctions.IsModuleLoaded("COMSVCS"))
		{
			if (Globals.g_OSVER == Globals.OS_VER_WIN2K)
			{
				num = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!STAThread::sm_cThreads"));
			}
			else if (Globals.g_OSVER >= Globals.OS_VER_WINXP)
			{
				num = ((!(Convert.ToString(Globals.g_OSPlatformVersion) == "X64")) ? Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!g_STAThreadPool+4")) : Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!g_STAThreadPool+8")));
			}
		}
		return num;
	}

	public long GetCOMPlusInstanceCount()
	{
		long num = 0L;
		num = 0L;
		if (Globals.HelperFunctions.IsModuleLoaded("COMSVCS"))
		{
			num = Convert.ToInt64(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("comsvcs!g_pAppTrackerObject"));
			if (num > 0)
			{
				num = ((!(Convert.ToString(Globals.g_OSPlatformVersion) == "X64")) ? Convert.ToInt64(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("poi(comsvcs!g_pAppTrackerObject)+10")) : Convert.ToInt64(Globals.HelperFunctions.GetDwordAtSymbolNoErrors("poi(comsvcs!g_pAppTrackerObject)+1c")));
			}
		}
		return num;
	}

	public int GetHostApartmentID(string sHostPrefix)
	{
		int num = 0;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		int num2 = 0;
		int num3 = 0;
		num = 0;
		Globals.g_HostOffsetHex = "";
		if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule)))
		{
			if (sHostPrefix == "MAIN")
			{
				num = Convert.ToInt32(GetMainSTAApartmentID());
			}
			else
			{
				if (Globals.g_OSVER <= Globals.OS_VER_WIN7)
				{
					Globals.g_ModuleCache.GetModuleByName(Globals.g_COMRuntimeModule).GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
					if (Globals.g_OSVER == Globals.OS_VER_WIN2K)
					{
						num2 = 2195;
						num3 = 6892;
					}
					else if (Globals.g_OSVER == Globals.OS_VER_WINXP)
					{
						num2 = 2600;
						num3 = 1322;
					}
					else
					{
						num2 = 0;
						num3 = 0;
					}
					if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
					{
						Globals.g_HostOffsetHex = "34";
					}
					else if (Convert.ToInt32(Build) > num2 || (Convert.ToInt32(Build) == num2 && Convert.ToInt32(Priv) >= num3))
					{
						Globals.g_HostOffsetHex = "20";
					}
					else
					{
						Globals.g_HostOffsetHex = "18";
					}
				}
				else if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
				{
					Globals.g_HostOffsetHex = "38";
				}
				else
				{
					Globals.g_HostOffsetHex = "24";
				}
				num = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors(Globals.g_COMRuntimeModule + "!g" + sHostPrefix + "Host+0x" + Globals.g_HostOffsetHex));
			}
		}
		return num;
	}

	private int GetMainSTAApartmentID()
	{
		int num = 0;
		num = 0;
		if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded(Globals.g_COMRuntimeModule)))
		{
			num = Convert.ToInt32(Globals.HelperFunctions.GetDwordAtSymbolNoErrors(Globals.g_COMRuntimeModule + "!gdwMainThreadId"));
		}
		return num;
	}

	private string[] GetSTAThreadPoolThreadArray()
	{
		string text = null;
		string[] array = null;
		string text2 = null;
		text = GetSTATIDAndActivityCountCmd();
		if (Convert.ToString(text) == "")
		{
			return null;
		}
		Globals.g_Debugger.Execute("~0s");
		text2 = Globals.HelperFunctions.DebuggerExecuteReplaceLF(text, "  ");
		if (Convert.ToString(text2).ToLower().IndexOf("error", 0) >= 0)
		{
			return null;
		}
		array = text2.Split(new string[1] { "  " }, StringSplitOptions.None);
		if (array.Length < 4)
		{
			return null;
		}
		return array;
	}
}
