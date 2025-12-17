using System;
using System.Collections.Generic;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;

namespace DebugDiag.AnalysisRules;

public class PerfAnalysis : IMultiDumpRule, IAnalysisRuleBase, IAnalysisRuleMetadata, IMultiDumpRuleFilter
{
	public string Category => "Performance Analyzers";

	public string Description => "Performance analysis for multiple consecutive dumps of the same process";

	public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
	{
		Globals.Manager = manager;
		CDump cDump = null;
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		Globals.g_Progress = progress;
		Globals.g_Progress.SetOverallRange(0, Globals.PERF_ANALYSIS_STEP_COUNT_OVERALL + dumpFiles.Count * Globals.PERF_ANALYSIS_STEP_COUNT_REPEAT_EACH_DUMP);
		Globals.g_Progress.OverallPosition = 0;
		Globals.g_Progress.OverallStatus = "";
		Globals.g_Progress.CurrentStatus = "";
		Globals.g_Progress.CurrentPosition = 0;
		Globals.g_OverallProgress = 0;
		Globals.g_AllOperations = new COperations();
		Globals.g_dumps = new CDumps();
		Globals.g_Progress.OverallStatus = "Verifying all " + dumpFiles.Count + " dumps";
		if (VerifyAndSortDumps())
		{
			int count = Globals.g_dumps.Count;
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.g_HideDotNetReportInfo = true;
			Globals.AnalyzeManaged.InitClrGlobals();
			Globals.AnalyzeManaged.LoadCLRInformation(ignoreMiniDumpFailure: true);
			if (count < dumpFiles.Count)
			{
				Globals.g_Progress.SetOverallRange(0, Globals.PERF_ANALYSIS_STEP_COUNT_OVERALL + Globals.g_dumps.Count * Globals.PERF_ANALYSIS_STEP_COUNT_REPEAT_EACH_DUMP);
			}
			for (int i = 1; i <= count; i++)
			{
				cDump?.CloseDebugger();
				cDump = Globals.g_dumps.DumpBySortedDumpNumber(i);
				cDump.MakeCurrent();
				_ = manager.SourceInfoEnabled;
				AnalyzeThreads.PreloadThreadData(i, count);
				Globals.HelperFunctions.UpdateOverallProgress();
				PerfFunctions.LoadOperationsForDump(cDump, i, count);
				Globals.HelperFunctions.UpdateOverallProgress();
			}
			Globals.HelperFunctions.ResetStatusNoIncrement("Running performance analysis on all operations");
			PerfFunctions.AnalyzeOperations();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ResetStatusNoIncrement("Reporting all operations");
			PerfFunctions.ReportAllOperations();
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.HelperFunctions.ShowTraceData();
		}
	}

	private bool VerifyAndSortDumps()
	{
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		if (dumpFiles.Count >= Globals.MIN_PERF_DUMPS_FOR_ANALYSIS)
		{
			Globals.HelperFunctions.ResetStatus("Verifying dumps", dumpFiles.Count, "Dump");
			foreach (string item in dumpFiles)
			{
				Globals.HelperFunctions.IncrementSubStatus();
				Globals.g_dumps.Add(item, Globals.VERIFY_TARGET_PROCESS, bNotify: true);
				Globals.g_dumps.DumpBySortedDumpNumber(Globals.g_dumps.Count).MakeCurrent();
			}
			Globals.HelperFunctions.ClearSubStatus();
		}
		if (Globals.g_dumps.Count >= Globals.MIN_PERF_DUMPS_FOR_ANALYSIS)
		{
			Globals.g_dumps.Sort();
			Globals.g_dumps.DropSymSrv();
			Globals.g_dumps.SaveModulesAndAppendExePaths();
		}
		else
		{
			Globals.Manager.ReportError("Not enough dumps were selected for analysis. The PerfAnalysis script is designed to analyze multiple dumps of the same process and highlight commonalities between the dumps which may represent performance bottlenecks in the application.", "Please re-run the analysis and select <b>multiple</b> dump files of the <b>same</b> process.  The more dumps selected for analysis, the more accurate the results will be.", 1000, "{00904d46-2da7-46ef-be29-7eb72a395396}");
		}
		if (Globals.g_dumps.Count < Globals.MIN_PERF_DUMPS_FOR_ANALYSIS)
		{
			return false;
		}
		return true;
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
