using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;

namespace DebugDiag.AnalysisRules;

public class CDumps
{
	private string m_targetExecutableName = "";

	private int m_targetPID;

	private Dictionary<string, CDump> m_dumpsByLongFileName;

	private Dictionary<int, CDump> m_dumpsByTime;

	private Dictionary<double, string> m_modules;

	private bool m_moduleMismatchFound;

	public int Count => m_dumpsByLongFileName.Count;

	public CDumps()
	{
		m_dumpsByLongFileName = new Dictionary<string, CDump>();
		m_dumpsByTime = new Dictionary<int, CDump>();
		m_modules = new Dictionary<double, string>();
	}

	public CDump DumpBySortedDumpNumber(int dumpNumber)
	{
		CDump result = null;
		if (m_dumpsByTime.ContainsKey(dumpNumber))
		{
			result = m_dumpsByTime[dumpNumber];
		}
		return result;
	}

	public CDump DumpByLongFileName(string longFileName)
	{
		CDump result = null;
		string key = longFileName.ToUpper();
		if (m_dumpsByLongFileName.ContainsKey(key))
		{
			result = m_dumpsByLongFileName[key];
		}
		return result;
	}

	public void Add(string DataFile, bool bVerifyTarget, bool bNotify)
	{
		bool flag = false;
		CDump cDump = null;
		string key = DataFile.ToUpper();
		if (m_dumpsByLongFileName.ContainsKey(key))
		{
			throw new Exception("CDumps::Add :Dump " + Convert.ToString(cDump.LongFileName) + " already exists in collection");
		}
		cDump = new CDump();
		cDump.Init(DataFile);
		if (cDump.Debugger == null)
		{
			if (bNotify)
			{
				Globals.Manager.ReportWarning("Unable to open the file " + Convert.ToString(DataFile) + " for analysis. The file is probably corrupt.", "It is recommended that you get another dump of the target process when the issue occurs to ensure accurate data is reported.", 777, "{998560c0-b289-449d-90a3-0d25972aeb69}");
			}
			return;
		}
		if (!bVerifyTarget || VerifyDumpTarget(cDump, bNotify))
		{
			m_dumpsByLongFileName.Add(key, cDump);
			m_dumpsByTime.Add(m_dumpsByTime.Count + 1, cDump);
		}
		cDump.CloseDebugger();
	}

	private bool VerifyDumpTarget(CDump Dump, bool bNotify)
	{
		bool flag = false;
		string text = "";
		int num = 0;
		NetDbgObj val = null;
		bool flag2 = false;
		val = Dump.Debugger;
		if (val.IsCrashDump && bNotify)
		{
			Globals.Manager.ReportWarning("DebugDiag determined that this dump file (" + Convert.ToString(Dump.ShortFileName) + ") is a crash dump, not a hang dump.  Performance analysis will still be run since the target process matches the target process of the other dumps selected for analysis", "If this dump was selected by mistake, then re-run the performance analysis without selecting this dump.", 777, "{04900d18-6300-4c7a-a7b0-af9b1a465436}");
		}
		text = val.ExecutableName.ToUpper();
		num = val.ProcessID;
		if (m_targetExecutableName.Equals(""))
		{
			flag = true;
			m_targetExecutableName = text;
			m_targetPID = num;
			foreach (CacheFunctions.ScriptModuleClass module in val.Modules)
			{
				m_modules.Add(module.Base, module.ModuleName.ToUpper());
			}
		}
		else
		{
			flag = text.Equals(m_targetExecutableName) && num.Equals(m_targetPID);
			if (!m_moduleMismatchFound)
			{
				flag2 = true;
				string text2 = "";
				text2 = val.Execute(".sympath").ToUpper();
				if (text2.Substring(0, 23) == "SYMBOL SEARCH PATH IS: ")
				{
					text2 = text2.Substring(23);
				}
				if (val.Modules.Count == m_modules.Count)
				{
					foreach (CacheFunctions.ScriptModuleClass module2 in val.Modules)
					{
						if (m_modules.ContainsKey(module2.Base) && Convert.ToString(m_modules[module2.Base]) == Convert.ToString(module2.ModuleName).ToUpper())
						{
							flag2 = false;
						}
						if (flag2)
						{
							break;
						}
					}
				}
				if (text2 != "")
				{
					val.Execute(".sympath " + text2);
				}
				m_moduleMismatchFound = flag2;
			}
			if (!flag && bNotify)
			{
				Globals.Manager.ReportOther("DebugDiag determined that this dump file (" + Convert.ToString(Dump.ShortFileName) + ") is not a dump of the same target process as the other dumps that were selected for analysis, and did not include this dump in the performance analysis. If you wish to diable  this check, edit the PerfAnalysis.aspx script (located in the DebugDiag\\Scripts folder) and set the <b>VERIFY_TARGET_PROCESS</b> constant to <font color='Red'><b>False</b></font>.<br><br>Target process of this dump:&nbsp;&nbsp;" + text + " (PID " + Convert.ToString(num) + ")<br>Target process of other dumps:&nbsp;&nbsp;" + m_targetExecutableName + " (PID " + Convert.ToString(m_targetPID) + ")<br>", "", "Notification", "notificationicon.png", 777, "{cdf42efb-978f-412a-83a6-68fef48e228a}");
			}
		}
		return flag;
	}

	public void Sort()
	{
		Dictionary<int, CDump> dictionary = null;
		Dictionary<int, CDump> dictionary2 = null;
		double num = 0.0;
		int num2 = 0;
		double num3 = 0.0;
		CDump cDump = null;
		bool flag = false;
		int num4 = 0;
		dictionary2 = m_dumpsByTime;
		dictionary = new Dictionary<int, CDump>();
		Globals.HelperFunctions.ResetStatus("Sorting dumps", dictionary2.Count, "Sort Pass");
		while (dictionary2.Count > 0)
		{
			Globals.HelperFunctions.IncrementSubStatus();
			num2 = -1;
			foreach (int key in dictionary2.Keys)
			{
				cDump = dictionary2[key];
				num = cDump.ProcessUpTime;
				flag = false;
				if (num2 == -1)
				{
					flag = true;
				}
				else if (num < num3)
				{
					flag = true;
				}
				if (flag)
				{
					num3 = num;
					num2 = key;
				}
			}
			cDump = dictionary2[num2];
			num4 = (cDump.DumpNumber = dictionary.Count + 1);
			dictionary.Add(num4, cDump);
			dictionary2.Remove(num2);
		}
		m_dumpsByTime = dictionary;
		Globals.HelperFunctions.ClearSubStatus();
	}

	public void DropSymSrv()
	{
		if (Count <= 0 || m_moduleMismatchFound)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatusNoIncrement("Preloading symbol files (this may take a while)...");
		NetDbgObj debugger = DumpBySortedDumpNumber(1).Debugger;
		debugger.Execute("~*k1000");
		foreach (NetDbgThread thread in debugger.Threads)
		{
			foreach (NetDbgStackFrame item in (List<NetDbgStackFrame>)(object)thread.StackFrames)
			{
				debugger.GetSymbolFromAddress(item.InstructionAddress);
			}
		}
		foreach (int key in m_dumpsByTime.Keys)
		{
			m_dumpsByTime[key].DropSymSrv();
		}
		Globals.HelperFunctions.ClearSubStatus();
	}

	private void CheckIsWow64Dump()
	{
		Globals.g_IsWow64Dump = false;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		scriptModuleClass = Globals.g_ModuleCache.GetModuleByName("NTDLL");
		if (scriptModuleClass != null)
		{
			Globals.g_IsWow64Dump = scriptModuleClass.ImageName.ToUpper().IndexOf("\\WINDOWS\\SYSWOW64") >= 0;
		}
	}

	private string MapToSysWowIfNeeded(string path)
	{
		string text = "";
		text = path;
		if (!Globals.g_IsWow64Dump)
		{
			return text;
		}
		if (path.ToUpper().IndexOf("\\WINDOWS\\SYSTEM32") >= 0)
		{
			path = Globals.HelperFunctions.Replace(path.ToUpper(), "\\WINDOWS\\SYSTEM32", "\\WINDOWS\\SYSWOW64");
			if (File.Exists(path))
			{
				text = path;
			}
		}
		return text;
	}

	private bool SomeDotNetImageFilesAreMissingOrMismatched(NetDbgObj debugger)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		bool flag = false;
		string text = null;
		string text2 = null;
		string text3 = null;
		CheckIsWow64Dump();
		flag = true;
		foreach (IDbgModule module in debugger.Modules)
		{
			IDbgModule val = module;
			text = val.ImageName;
			if (text.IndexOf("\\") >= 0)
			{
				text = MapToSysWowIfNeeded(val.ImageName);
			}
			if (!File.Exists(text))
			{
				return flag;
			}
			text2 = val.VSFileVersion;
			text3 = FileVersionInfo.GetVersionInfo(text).FileVersion;
			if (!DoVersionsMatch(ref text2, ref text3))
			{
				return flag;
			}
		}
		return false;
	}

	private bool DoVersionsMatch(ref string v1, ref string v2)
	{
		bool result = false;
		int num = 0;
		string[] array = null;
		string[] array2 = null;
		int num2 = 0;
		if (v1.GetSafeLength() == 0)
		{
			return result;
		}
		if (v2.GetSafeLength() == 0)
		{
			return result;
		}
		if (v1.ToUpper() == v2.ToUpper())
		{
			return true;
		}
		v1 = Globals.HelperFunctions.Split(v1, " ", -1)[0];
		v2 = Globals.HelperFunctions.Split(v2, " ", -1)[0];
		array = Globals.HelperFunctions.Split(v1, ".", -1);
		array2 = Globals.HelperFunctions.Split(v2, ".", -1);
		num2 = Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1);
		if (num2 > Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1))
		{
			num2 = Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1);
		}
		for (num = 0; num <= num2; num++)
		{
			if (array[num] != array2[num])
			{
				if (array[num].ToUpper() == "O")
				{
					array[num] = "0";
				}
				if (array2[num].ToUpper() == "O")
				{
					array2[num] = "0";
				}
				if (Convert.ToInt64(array[num]) != Convert.ToInt64(array2[num]))
				{
					return result;
				}
			}
		}
		return true;
	}

	public void SaveModulesAndAppendExePaths()
	{
		bool flag = false;
		int num = 0;
		string text = "";
		int key = 0;
		string text2 = "";
		string text3 = null;
		string text4 = "";
		bool flag2 = false;
		string text5 = "";
		NetDbgObj val = null;
		string text6 = "";
		Globals.AnalyzeManaged.FindCLRDLL();
		if (Globals.g_Debugger.ClrRuntime == null || Count <= 0)
		{
			return;
		}
		foreach (int key2 in m_dumpsByTime.Keys)
		{
			key = key2;
			if (m_dumpsByTime[key2].IsMiniDump)
			{
				flag = true;
				if (num != 0)
				{
					break;
				}
			}
			else
			{
				num = key2;
				if (flag)
				{
					break;
				}
			}
		}
		if (!SomeDotNetImageFilesAreMissingOrMismatched(m_dumpsByTime[key].Debugger))
		{
			return;
		}
		if (num == 0)
		{
			text3 = m_dumpsByTime[key].Debugger.EnvironmentVariables[(object)"ComputerName"];
			if (text3 != "")
			{
				text4 = Globals.HelperFunctions.GetAnalysisProcessEnvVar("ComputerName");
				if (text3 != text4)
				{
					flag2 = true;
				}
			}
			text6 = "If .NET call stacks are missing or incomplete, ";
			if (flag2)
			{
				text6 = text6 + "either perform the analysis on the original machine where the dump files were collected (" + Convert.ToString(text3) + "), or ";
			}
			text6 += "include at least one full dump when performing the analysis.";
			Globals.Manager.ReportError("Only mini dumps were selected for analysis. At least one full dump is required for managed (.NET) applications if the analysis is being performed on a machine where the image files are unavailable.", text6, 1000, "{00904d46-2da7-46ef-be29-7eb72a395397}");
			return;
		}
		val = m_dumpsByTime[num].Debugger;
		text3 = val.EnvironmentVariables[(object)"ComputerName"];
		text2 = Globals.HelperFunctions.GetAnalysisProcessEnvVar("Temp");
		if (Globals.g_ShortDumpFileName.IndexOf("_Time_") == -1)
		{
			text5 = "_" + Convert.ToString(val.SystemUpTime) + "_";
		}
		text = text2 + "\\DDModules\\" + Path.GetFileName(Globals.g_ShortDumpFileName) + text5 + "_" + Convert.ToString(text3);
		if (!Globals.HelperFunctions.FolderExists(text) || Directory.GetFiles(text).Length == 0)
		{
			Directory.CreateDirectory(text);
			m_dumpsByTime[num].SaveAllModules(text);
		}
		foreach (int key3 in m_dumpsByTime.Keys)
		{
			if (m_dumpsByTime[key3].IsMiniDump)
			{
				m_dumpsByTime[key3].AppendExePath(text);
			}
		}
	}
}
