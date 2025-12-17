using System;
using CrashHangExtLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.Reports;
using IISInfoLib;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

public class CrashHangAnalysis : IMultiDumpRule, IAnalysisRuleBase, IAnalysisRuleMetadata, IMultiDumpRuleFilter
{
	public string Category => "Default Analysis";

	public string Description => "Default crash and hang analysis for all dumps.  Includes specific reporting for ASP.NET, WCF, IIS, and more";

	public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
	{
		Globals.Manager = manager;
		Globals.g_Progress = progress;
		Globals.g_Progress.SetOverallRange(0, Globals.Manager.GetDumpFiles().Count * Globals.ANALYSIS_STEP_COUNT);
		Globals.HelperFunctions.LoadProcessesInThisReport();
		Globals.HelperFunctions.ResetStatusNoIncrement("Starting Analysis");
		foreach (string dumpFile in Globals.Manager.GetDumpFiles())
		{
			Globals.g_DataFile = dumpFile;
			if (!(Globals.HelperFunctions.UCase(Globals.HelperFunctions.Right(Globals.g_DataFile, 3)) == "DMP"))
			{
				continue;
			}
			Globals.g_Debugger = null;
			NetDbgObj val = (Globals.g_Debugger = Globals.Manager.GetDebugger(Globals.g_DataFile));
			try
			{
				CacheFunctions.ResetCache();
				if (Globals.g_Debugger != null)
				{
					if (Globals.g_Debugger.IsKernelMode)
					{
						manager.WriteLine($"The CrashHangAnalysis rule applies only to <b>usermode</b> dumps.  <font color='red'>Skipping analysis for <b>kernel</b> dump file <b>{Globals.g_ShortDumpFileName}</b></font>");
						continue;
					}
					Globals.HelperFunctions.SetOSVersion();
					Globals.g_ShortDumpFileName = Globals.HelperFunctions.LongNameFromShortName(Globals.g_DataFile);
					Globals.HelperFunctions.ResetStatusNoIncrement("Loading debug extensions");
					Globals.g_UtilExt = Globals.g_Debugger.GetExtensionObject("CrashHangExt", "Utils") as Utils;
					Globals.g_VMInfo = Globals.g_Debugger.GetExtensionObject("MemoryExt", "VMInfo") as VMInfo;
					Globals.g_HTTPInfo = Globals.g_Debugger.GetExtensionObject("IISInfo", "HTTPInfo") as IHTTPInfo;
					Globals.g_ASPInfo = Globals.g_Debugger.GetExtensionObject("IISInfo", "ASPInfo") as IASPInfo;
					Globals.g_LeakTrackInfo = Globals.g_VMInfo.LeakTrackInfo;
					Globals.g_HeapInfo = Globals.g_VMInfo.HeapInfo;
					Globals.g_ExtendedThreadInfoAvailable = Globals.g_Debugger.ExtendedThreadInfoAvailable;
					int count = Globals.Manager.GetResults(0).Count;
					CacheFunctions.ResetCache();
					if (!Globals.g_UtilExt.IsPEBValid)
					{
						if (Globals.g_Debugger.DumpType != "MINIDUMP")
						{
							Globals.Manager.ReportOther("<br>DebugDiag failed to locate the PEB (Process Environment Block) in <b>" + Globals.g_ShortDumpFileName + "</b>, and as a result, debug analysis for this dump may be incomplete or inaccurate.<br><br>", "It is recommended that you get another dump of the target process when the issue occurs to ensure accurate data is reported", "Notification", "notificationicon.png", 0, "{282f95cc-ca73-447e-91a1-ee72a105ada2}");
						}
						Globals.g_GlobalFlagsValue = 0;
					}
					else
					{
						Globals.g_GlobalFlagsValue = (int)Globals.HelperFunctions.FromHex(Globals.g_UtilExt.NTGlobalFlags);
					}
					Globals.g_collCritSecs.Clear();
					Globals.g_collThreadsBlockedByCritsecs.Clear();
					Globals.g_collExceptionThreads.Clear();
					Globals.g_collDeadLockedThreads.Clear();
					Globals.g_collKnownCSIssueFound.Clear();
					Globals.g_collPreviousExceptions.Clear();
					CacheFunctions.ResetCache();
					Globals.AnalyzeManaged.InitClrGlobals();
					Globals.g_LongRunningClientConns = 0;
					Globals.g_LongRunningASPReq = 0;
					ReportSection val2 = Globals.Manager.AddReportSection(Globals.g_ShortDumpFileName, (SectionType)1);
					val2.Title = "Report for " + Globals.g_ShortDumpFileName;
					Globals.Manager.CurrentSection = val2;
					Globals.g_UniqueReference = val2.GetUID;
					if (Globals.g_Debugger.IsCrashDump && Globals.HelperFunctions.Not(Globals.Manager.DoHangAnalysisOnCrashDumps))
					{
						Globals.HelperFunctions.ResetStatusNoIncrement("Running crash analysis on " + Globals.g_ShortDumpFileName);
						Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, "Crash Analysis");
						AnalyzeCrash.AnalyzeExceptionThread(Globals.g_ThreadInfoCache.Item(Globals.g_Debugger.ExceptionThread.ThreadID));
						Globals.Manager.ReportOther("DebugDiag determined that this dump file (" + Globals.g_ShortDumpFileName + ") is a <b>crash</b> dump and did not perform any <b>hang</b> analysis.", "To run both hang rules and crash rules on crash dumps, select the following option in the <i>'Preferences'</i> tab of the <i>'Settings'</i> page in DebugDiag.Analysis.exe:<br><br>&nbsp;&nbsp;&nbsp;&nbsp;<i>'For crash dumps, run hang rules and crash rules'</i>\n", "Notification", "notificationicon.png", 10000, "{71d3fb61-c61a-4912-83d7-1b7d0af2520a}");
					}
					else if (Globals.g_Debugger.IsCrashDump && Globals.Manager.DoHangAnalysisOnCrashDumps)
					{
						Globals.HelperFunctions.ResetStatusNoIncrement("Running combined crash/hang analysis on " + Globals.g_ShortDumpFileName);
						Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, "Combined Crash/Hang Analysis");
						if (DoHangAnalysis())
						{
							DoHangReport();
						}
						AnalyzeCrash.AnalyzeExceptionThread(Globals.g_ThreadInfoCache.Item(Globals.g_Debugger.ExceptionThread.ThreadID));
					}
					else
					{
						Globals.HelperFunctions.ResetStatusNoIncrement("Running hang analysis on " + Globals.g_ShortDumpFileName);
						Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, "Hang Analysis");
						if (DoHangAnalysis())
						{
							DoHangReport();
						}
					}
					CheckJVM();
					if (count == Globals.Manager.GetResults(0).Count)
					{
						if (Globals.Manager.GetDumpFiles().Count == 1)
						{
							Globals.Manager.ReportOther("<br>DebugDiag did not detect any known problems in " + Globals.g_ShortDumpFileName + " using the current set of rules.<br><br>", "", "Notification", "notificationicon.png", 0, "{eaf17259-74a7-4b87-8d03-ae0357bc583f}");
						}
						else
						{
							QueuedReportGroupClass group = Globals.g_QueuedReports.GetGroup("NoProblem");
							if (Globals.HelperFunctions.Len(group.Message) == 0)
							{
								group.Consolidated = true;
								group.Message = "<br>DebugDiag did not detect any known problems in the following dump files:";
							}
							QueuedReportClass report = group.GetReport(Globals.g_ShortDumpFileName);
							report.Visible = false;
							report.UniqueKey = Globals.g_UniqueReference;
						}
					}
					Globals.Manager.CurrentSection = val2.Parent;
				}
				else
				{
					Globals.Manager.ReportError("Unable to open the file " + Globals.g_DataFile + " for analysis.", "The file may be corrupt, in which case a new dump file of the targeted process will have to be created to do any analysis.", 0, "{cbdb676b-5984-4889-ad61-639cb79d78d7}");
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		Globals.g_QueuedReports.WriteSummaries();
		Globals.HelperFunctions.ShowTraceData();
	}

	public bool DoHangAnalysis()
	{
		ReportSection currentSection = Globals.Manager.CurrentSection;
		Globals.g_oldVBRuntime = AnalyzeVBModInfo.GetVBRTVer();
		Globals.g_IsDebugEnabled = Globals.ReportASPInfo.IsASPDebuggingEnabled();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeManaged.FindCLRDLL();
		if (Globals.g_Debugger.ClrRuntime != null)
		{
			ReportSection val = currentSection.AddChildSection("NetAnalysisReport", (SectionType)0);
			val.Title = ".Net Analysis Report";
			Globals.Manager.CurrentSection = val;
		}
		Globals.AnalyzeManaged.LoadCLRInformation();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.Manager.CurrentSection = currentSection;
		AnalyzeThreads.PreloadThreadData(0, 0);
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeCritSecs.AnalyzeRootLocks();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeCritSecs.AnalyzeCritSecs();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeComPlus.AnalyzeComAndComPlusInfo();
		Globals.HelperFunctions.UpdateOverallProgress();
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			Globals.Manager.CurrentSection = currentSection.GetInnerSection("NetAnalysisReport", (SectionType)0);
			Globals.HelperFunctions.ResetStatusNoIncrement("Finding all known .NET Threads");
			Globals.AnalyzeManaged.FindCLRKnownThreads();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Collecting detailed information for .NET Threads");
			Globals.AnalyzeManaged.DisplayBangThreads();
			Globals.AnalyzeManaged.DisplayBangThreadPool();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Finding all Http Runtimes which have debug set to true");
			Globals.AnalyzeManaged.FindDebugTrue();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking for known .NET Garbage Collection issues");
			Globals.AnalyzeManaged.GetGarbageCollectionInformation();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Finding all HttpContext objects");
			Globals.AnalyzeManaged.PrintHttpContextInformation();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Finding previous .NET exceptions in all .NET Thread Stacks");
			Globals.AnalyzeManaged.FindManagedExceptionsForAllThreads();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Getting information about the WCF threads");
			Globals.AnalyzeManaged.GetWCFThreadsInformation();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Finding previous .NET exceptions in all .NET Heaps");
			Globals.AnalyzeManaged.ReportAllExceptions();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.AnalyzeManaged.FindSyncBlk();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking for known .NET ThreadPool issues");
			Globals.AnalyzeManaged.CheckThreadPool();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking for known WCF Throttling issues");
			Globals.AnalyzeManaged.AnalyzeWCFServiceHost();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Displaying WCF request summary");
			Globals.AnalyzeManaged.AnalyzeWCFRequest();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Generating ADO.NET Report");
			Globals.AnalyzeManaged.GenerateADODotNetReport();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking for request queueing in the WebEngine queue");
			Globals.AnalyzeManaged.CheckWebEngineQueue();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking for all modules compiled in DEBUG mode");
			Globals.AnalyzeManaged.PrintDebugModuleInformation();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Checking buffered System.Net Connections");
			Globals.AnalyzeManaged.CheckBufferedSystemDotNetConnections();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Building .net Analysis Report");
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.Manager.CurrentSection = currentSection;
		}
		else
		{
			for (int i = 1; i <= 12; i++)
			{
				Globals.HelperFunctions.UpdateOverallProgress();
			}
		}
		Globals.AnalyzeThreads.DoAnalyzeThreads();
		Globals.HelperFunctions.UpdateOverallProgress();
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			Globals.AnalyzeManaged.AnalyzeBlockedFinalizer();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.AnalyzeManaged.CheckLeakedSystemDotNetConnections();
			Globals.HelperFunctions.UpdateOverallProgress();
		}
		Globals.HelperFunctions.UpdateOverallProgress();
		return true;
	}

	public static void DoHangReport()
	{
		Globals.AnalyzeCritSecs.ReportCritSecs();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeThreads.ReportThreads();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeComPlus.ReportCOMPlusInfo();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.AnalyzeComPlus.ReportCOMInfo();
		Globals.HelperFunctions.UpdateOverallProgress();
		if (Globals.g_Debugger.DumpType != "MINIDUMP")
		{
			Globals.ReportHTTPInfo.Report_HTTPInfo();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.ReportASPInfo.Report_ASPInfo();
			Globals.HelperFunctions.UpdateOverallProgress();
			if (Globals.g_LongRunningClientConns > 0 || Globals.g_LongRunningASPReq > 0)
			{
				Globals.ReportHTTPInfo.ReportLongRunningRequests();
			}
		}
		else
		{
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.UpdateOverallProgress();
		}
		if (Globals.HelperFunctions.Not(Globals.g_oldVBRuntime == ""))
		{
			AnalyzeVBModInfo.ReportVBDllInfo();
		}
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.UpdateOverallProgress();
	}

	public void CheckJVM()
	{
		if (Globals.g_ModuleCache.GetModuleByName("msjava") != null)
		{
			Globals.Manager.ReportWarning("The MSJVM reached the end of its life as of June 30, 2009. Customers are encouraged to take proactive steps and move away from the MSJVM as it is an obsolete software. The MSJVM is no longer available for distribution from Microsoft and there will be no enhancements to the MSJVM. Microsoft products and SKUs currently including the MSJVM have been retired or replaced by versions not containing the MSJVM.", "You can contact Microsoft to obtain the MSJVM removal tool which is referenced here: <a href='http://support.microsoft.com/kb/826878'>http://support.microsoft.com/kb/826878</a> The removal tool will remove the MSJVM dependency from your system", 0, "{077fe285-6c80-4462-858c-067ea6293ba6}");
		}
	}

	public static void InitForUnitTest(NetScriptManager manager, NetProgress progress, string dumpPath, bool preloadThreadData)
	{
		Globals.Manager = manager;
		Globals.g_Progress = progress;
		Globals.g_DataFile = dumpPath;
		if (!(Globals.HelperFunctions.UCase(Globals.HelperFunctions.Right(Globals.g_DataFile, 3)) == "DMP"))
		{
			return;
		}
		Globals.g_Debugger = null;
		Globals.HelperFunctions.LoadProcessesInThisReport();
		Globals.g_Debugger = Globals.Manager.GetDebugger(Globals.g_DataFile);
		if (Globals.g_Debugger == null)
		{
			return;
		}
		Globals.HelperFunctions.SetOSVersion();
		Globals.g_ShortDumpFileName = Globals.HelperFunctions.LongNameFromShortName(Globals.g_DataFile);
		Globals.g_UtilExt = Globals.g_Debugger.GetExtensionObject("CrashHangExt", "Utils") as Utils;
		Globals.g_VMInfo = Globals.g_Debugger.GetExtensionObject("MemoryExt", "VMInfo") as VMInfo;
		Globals.g_HTTPInfo = Globals.g_Debugger.GetExtensionObject("IISInfo", "HTTPInfo") as IHTTPInfo;
		Globals.g_ASPInfo = Globals.g_Debugger.GetExtensionObject("IISInfo", "ASPInfo") as IASPInfo;
		Globals.g_LeakTrackInfo = Globals.g_VMInfo.LeakTrackInfo;
		Globals.g_HeapInfo = Globals.g_VMInfo.HeapInfo;
		Globals.g_ExtendedThreadInfoAvailable = Globals.g_Debugger.ExtendedThreadInfoAvailable;
		Globals.g_UniqueReference = Globals.HelperFunctions.GetUniqueReference(Globals.g_Debugger);
		CacheFunctions.ResetCache();
		if (!Globals.g_UtilExt.IsPEBValid)
		{
			Globals.g_GlobalFlagsValue = 0;
		}
		else
		{
			Globals.g_GlobalFlagsValue = (int)Globals.HelperFunctions.FromHex(Globals.g_UtilExt.NTGlobalFlags);
		}
		Globals.g_collCritSecs.Clear();
		Globals.g_collThreadsBlockedByCritsecs.Clear();
		Globals.g_collExceptionThreads.Clear();
		Globals.g_collDeadLockedThreads.Clear();
		Globals.g_collKnownCSIssueFound.Clear();
		Globals.g_collPreviousExceptions.Clear();
		CacheFunctions.ResetCache();
		Globals.AnalyzeManaged.InitClrGlobals();
		Globals.g_LongRunningClientConns = 0;
		Globals.g_LongRunningASPReq = 0;
		if (!Globals.g_Debugger.IsCrashDump || Globals.Manager.DoHangAnalysisOnCrashDumps)
		{
			Globals.g_oldVBRuntime = AnalyzeVBModInfo.GetVBRTVer();
			Globals.g_IsDebugEnabled = Globals.ReportASPInfo.IsASPDebuggingEnabled();
			Globals.AnalyzeManaged.LoadCLRInformation();
			Globals.HelperFunctions.UpdateOverallProgress();
			if (preloadThreadData)
			{
				AnalyzeThreads.PreloadThreadData(0, 0);
				Globals.HelperFunctions.UpdateOverallProgress();
			}
		}
	}

	public bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason)
	{
		foreach (string dumpFile in manager.GetDumpFiles())
		{
			NetDbgObj debugger = manager.GetDebugger(dumpFile);
			try
			{
				if (!debugger.IsKernelMode)
				{
					return true;
				}
			}
			finally
			{
				((IDisposable)debugger)?.Dispose();
			}
		}
		filterReason = "None of the selected dumps are usermode dumps";
		return false;
	}
}
