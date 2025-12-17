using System;
using System.Collections.Generic;
using System.Configuration;
using CrashHangExtLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.Reports;
using IISInfoLib;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

public class MemoryAnalysis : IMultiDumpRule, IAnalysisRuleBase, IAnalysisRuleMetadata, IMultiDumpRuleFilter
{
	private AnalysisModes _analysisMode;

	public string Category => "Memory Pressure Analyzers";

	public string Description => "Memory analysis including LeakTrack and heap info reporting";

	public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		Globals.Manager = manager;
		Globals.g_Progress = progress;
		Globals.g_Progress.OverallStatus = "";
		Globals.g_Progress.CurrentStatus = "";
		Globals.g_Progress.CurrentPosition = 0;
		Globals.g_OverallProgress = 0;
		Globals.g_StepCount = 3;
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		Globals.g_Progress.OverallStatus = "Building the table of contents";
		Globals.g_Progress.SetOverallRange(0, dumpFiles.Count * (Globals.g_StepCount + 1));
		foreach (string item in dumpFiles)
		{
			Globals.g_DataFile = item;
			if (Globals.g_DataFile.Substring(Globals.g_DataFile.GetSafeLength() - 4).ToUpper() == ".DMP")
			{
				Globals.g_ShortDumpFileName = Globals.g_DataFile.Substring(Globals.g_DataFile.LastIndexOf("\\") + 1);
				Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing " + Globals.g_ShortDumpFileName);
				try
				{
					Globals.g_Debugger = Globals.Manager.GetDebugger(Globals.g_DataFile);
					CacheFunctions.ResetCache();
					Globals.g_UtilExt = (Utils)Globals.g_Debugger.GetExtensionObject("CrashHangExt", "Utils");
					Globals.g_ASPInfo = (IASPInfo)Globals.g_Debugger.GetExtensionObject("IISInfo", "ASPInfo");
					Globals.g_VMInfo = (VMInfo)Globals.g_Debugger.GetExtensionObject("MemoryExt", "VMInfo");
					Globals.g_HeapInfo = Globals.g_VMInfo.HeapInfo;
					Globals.g_LeakTrackInfo = Globals.g_VMInfo.LeakTrackInfo;
					if (Globals.g_Debugger != null)
					{
						string filterReason = null;
						if (!ShouldRunAnalysis(Globals.g_Debugger, _analysisMode, ref filterReason))
						{
							manager.WriteLine($"{filterReason}.  Skipping analysis for dump file <b>{Globals.g_ShortDumpFileName}</b>");
							continue;
						}
						Globals.HelperFunctions.SetOSVersion();
						ReportSection val = Globals.Manager.AddReportSection(Globals.g_ShortDumpFileName, (SectionType)1);
						val.Title = "Report for " + Globals.g_ShortDumpFileName;
						Globals.Manager.CurrentSection = val;
						Globals.g_UniqueReference = val.GetUID;
						if (Globals.g_Debugger.DumpType != "MINIDUMP")
						{
							Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, "Memory Pressure Analysis");
							Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing virtual memory information");
							VMFunctions.AnalyzeAndReportVMInfo();
							Globals.HelperFunctions.UpdateOverallProgress();
							Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing heap information");
							Globals.HeapFunctions.AnalyzeAndReportHeapInfo();
							Globals.HelperFunctions.UpdateOverallProgress();
							if (Globals.g_LeakTrackInfo.IsLeakTrackLoaded)
							{
								Globals.HelperFunctions.ResetStatusNoIncrement("Loading Leak Track info. Please wait...");
								LeakFunctions.AnalyzeAndReportLTData();
							}
							else
							{
								Globals.Manager.ReportOther("DebugDiag did not detect LeakTrack.dll loaded in <b>" + Globals.g_ShortDumpFileName + "</b>, so no leak analysis was performed on this file. &nbsp;If you are troubleshooting a memory leak, please ensure LeakTrack.dll is injected into the target process using the DebugDiag tool before or generating new dumps.<br><br>For information regarding installation and usage of the IISDiag tool, please see the included help file.", "", "Notification", "notificationicon.png", 0, "{0d89870a-0e55-4568-9bbd-cc5df48e54f7}");
							}
							Globals.HeapFunctions.ShowHeapInfoNoneDetectedIfNecessary();
							Globals.HelperFunctions.UpdateOverallProgress();
						}
						Globals.Manager.CurrentSection = val.Parent;
					}
					else
					{
						for (int i = 0; i < Globals.g_StepCount; i++)
						{
							Globals.HelperFunctions.UpdateOverallProgress();
						}
					}
				}
				finally
				{
					if (Globals.g_Debugger != null)
					{
						Globals.g_Debugger.Dispose();
					}
				}
			}
			else
			{
				for (int j = 0; j < Globals.g_StepCount; j++)
				{
					Globals.HelperFunctions.UpdateOverallProgress();
				}
			}
		}
	}

	public bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		_analysisMode = mode;
		foreach (string dumpFile in manager.GetDumpFiles())
		{
			NetDbgObj val = NetDbgObj.OpenDump(dumpFile, (string)null, (string)null, (object)null, false, true, true);
			try
			{
				if (ShouldRunAnalysis(val, mode, ref filterReason))
				{
					return true;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		filterReason = "None of the selected dumps are full usermode dumps";
		if ((int)mode == 1)
		{
			filterReason += " with > 1GB Virtual Bytes in the process.";
		}
		else
		{
			filterReason += ".";
		}
		return false;
	}

	private bool ShouldRunAnalysis(NetDbgObj debugger, AnalysisModes mode, ref string filterReason)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (debugger.DumpType == "MINIDUMP")
		{
			filterReason = debugger.DumpFileShortName + " is a <b>mini</b> dump.  MemoryAnalysis requires a <b>full</b> usermode dump.";
			return false;
		}
		if (debugger.IsKernelMode)
		{
			filterReason = debugger.DumpFileShortName + " is a <b>kernel</b> dump.  MemoryAnalysis requires a full <b>usermode</b> dump.";
			return false;
		}
		if ((int)mode == 0)
		{
			return true;
		}
		if (IsVirtBytesHigh(debugger))
		{
			return true;
		}
		filterReason = "There are <b>less than 1GB Virtual Bytes</b> in this process.";
		return false;
	}

	private bool IsVirtBytesHigh(NetDbgObj dbgObj)
	{
		if (dbgObj.DumpType == "MINIDUMP")
		{
			return false;
		}
		IVMInfo obj = (IVMInfo)dbgObj.GetExtensionObject("MemoryExt", "VMInfo");
		string text = ConfigurationManager.AppSettings["VirtualBytesHighThreshold"];
		if (text == null || !double.TryParse(text, out var result))
		{
			result = 1073741824.0;
		}
		obj.HeapInfo.DoSearchForHeapOwners = false;
		double filteredBlockSize = obj.GetFilteredBlockSize(0, 0, 8192, 0.0);
		double filteredBlockSize2 = obj.GetFilteredBlockSize(0, 0, 4096, 0.0);
		obj.GetFilteredBlockSize(0, 0, 65536, 0.0);
		if (filteredBlockSize + filteredBlockSize2 > result)
		{
			return true;
		}
		return false;
	}
}
