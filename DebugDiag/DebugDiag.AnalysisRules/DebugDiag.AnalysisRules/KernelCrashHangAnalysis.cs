using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.HtmlHelpers;
using DebugDiag.DotNet.Kernel;

namespace DebugDiag.AnalysisRules;

public class KernelCrashHangAnalysis : IMultiDumpRule, IAnalysisRuleBase, IAnalysisRuleMetadata, IMultiDumpRuleFilter
{
	private const string lineDelimiter = "\n<br/>";

	private HashSet<string> ignoreFrames = new HashSet<string>();

	private List<string> ignorePatterns = new List<string>();

	public string Category => "Kernel";

	public string Description => " -- Preview -- Default crash and hang analysis for all kernel dumps ";

	public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
	{
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Expected O, but got Unknown
		foreach (string dumpFile in manager.GetDumpFiles())
		{
			NetDbgObj debugger = manager.GetDebugger(dumpFile);
			try
			{
				if (debugger.IsKernelMode)
				{
					uint bugCheckCode = debugger.BugCheck.BugCheckCode;
					string text = "!analyze -v";
					string text2 = debugger.Execute(text);
					text2 = text2.Replace("\nArguments:", "\n\nArguments:");
					text2 = text2.Replace("\nFollowup:", "\n\nFollowup:");
					string[] array = text2.Replace("\n\n", "ñ").Split('ñ');
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					string[] array2 = array;
					foreach (string text3 in array2)
					{
						string text4 = text3.Trim();
						if (text4.ToUpper().Contains($"({bugCheckCode:X})\n"))
						{
							dictionary["SUMMARY"] = text4;
						}
						else if (!string.IsNullOrEmpty(text4) && text4.Contains(":"))
						{
							int num = text3.IndexOf(':');
							string text5 = text3.Substring(0, num);
							if (!text5.Contains(" "))
							{
								string text6 = text3.Substring(num + 1);
								dictionary[text5] = text6.Trim();
							}
						}
					}
					text += " -xml";
					text2 = debugger.Execute(text);
					int num2 = text2.IndexOf("<ANALYSIS>");
					if (num2 > -1)
					{
						if (num2 > 0)
						{
							text2 = text2.Substring(num2);
						}
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.LoadXml("<?xml version=\"1.0\" ?>" + text2);
						foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
						{
							string name = childNode.Name;
							if (!dictionary.ContainsKey(name))
							{
								string innerXml = childNode.InnerXml;
								if (!(name == "FA_PERF"))
								{
									dictionary[name] = HttpUtility.HtmlEncode(innerXml);
								}
							}
						}
					}
					List<string> stackFrames = null;
					string functionName = null;
					GetFaultInfo(dictionary, debugger, out var moduleName, out var symbolName, out functionName, out var _, out stackFrames);
					string dumpFileShortName = debugger.DumpFileShortName;
					string description = GetDescription(debugger, bugCheckCode, dictionary, dumpFileShortName, symbolName);
					IDbgModule moduleByModuleName = debugger.GetModuleByModuleName(moduleName);
					string recommendation = GetRecommendation(debugger.BugCheck, dictionary, moduleByModuleName);
					manager.ReportError(description, recommendation, 0, "{E82EF6EB-2628-41E8-8D8C-2CA7A2A146DB}");
					HTMLTable val = new HTMLTable();
					val.AddRow(new object[2] { "Dump File", dumpFileShortName });
					val.AddRow(new object[2]
					{
						"Dump Type",
						debugger.IsKernelMode ? "Kernel" : "User"
					});
					DateTime createTime = debugger.CreateTime;
					val.AddRow(new object[2]
					{
						"Dump Time",
						createTime.ToShortDateString() + " " + createTime.ToShortTimeString()
					});
					manager.WriteLine(((object)val).ToString());
					val = new HTMLTable();
					foreach (KeyValuePair<string, string> item in dictionary)
					{
						string text7 = item.Value.Trim().Replace("\n", "<br/>");
						string key = item.Key;
						bool flag = false;
						if (key == "TRAP_FRAME" || key == "STACK_TEXT")
						{
							flag = true;
							text7 = $"<font face=\"consolas\">{text7}</font>";
						}
						val.AddRowNoWrap(new object[3] { flag, item.Key, text7 });
					}
					manager.WriteLine(((object)val).ToString().Replace("\r\n", ",<br>").Replace("\r", "<br>")
						.Replace("\n", "<br>"));
				}
				else
				{
					manager.WriteLine(string.Format("The KernelCrashAnalysis rule applies only to <b>kernel crash</b> dumps.  <font color='red'>Skipping analysis for <b>{1} {2}</b> dump file <b>{0}</b></font>", Globals.g_ShortDumpFileName, debugger.IsKernelMode ? "kernel" : "user", debugger.IsCrashDump ? "crash" : "hang"));
				}
			}
			finally
			{
				((IDisposable)debugger)?.Dispose();
			}
		}
	}

	private string GetRecommendation(BugCheckData bugCheckData, Dictionary<string, string> bangAnalyzeData, IDbgModule faultingModule)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (faultingModule != null)
		{
			stringBuilder.Append("Please ensure the latest version of <b>" + faultingModule.ModuleName + "</b> is installed<br><br>");
		}
		else
		{
			stringBuilder.Append("Provide this report to your system administrator or support professional for further assistance.");
		}
		return stringBuilder.ToString();
	}

	private string GetDescription(NetDbgObj debugger, uint bugCheckCode, Dictionary<string, string> bangAnalyzeData, string dumpFileShortName, string symbolName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendFormat("In <b>{0}</b> ", dumpFileShortName);
		if (!string.IsNullOrEmpty(symbolName))
		{
			stringBuilder.AppendFormat("the instruction at <b>{0}</b> caused ", symbolName);
		}
		else
		{
			stringBuilder.Append("the system encountered ");
		}
		stringBuilder.AppendFormat("a kernel <font color=red><b>BugCheck {0:X}</b></font> with the following details:", bugCheckCode);
		string value = null;
		if (!bangAnalyzeData.TryGetValue("SUMMARY", out value) || string.IsNullOrEmpty(value))
		{
			value = $"BugCheck Code 0x{bugCheckCode:X}\n";
		}
		int num = value.IndexOf('\n');
		if (num > -1)
		{
			value = $"<b>{value.Substring(0, num)}</b>{value.Substring(num)}";
		}
		stringBuilder.AppendFormat("<br><br>{0}", value.Replace("\n", "<br>"));
		string bugCheckArgsTable = GetBugCheckArgsTable(bangAnalyzeData, debugger.BugCheck);
		if (!string.IsNullOrEmpty(bugCheckArgsTable))
		{
			stringBuilder.AppendFormat("<br><br>{0}", bugCheckArgsTable);
		}
		return stringBuilder.ToString();
	}

	private void GetFaultInfo(Dictionary<string, string> bangAnalyzeData, NetDbgObj debugger, out string moduleName, out string symbolName, out string functionName, out bool fromBangAnalyze, out List<string> stackFrames)
	{
		functionName = null;
		symbolName = null;
		fromBangAnalyze = false;
		moduleName = null;
		if (bangAnalyzeData.TryGetValue("MODULE_NAME", out moduleName) && !string.IsNullOrEmpty(moduleName) && moduleName.ToUpper() != "UNKNOWN_MODULE" && !bangAnalyzeData.TryGetValue("SYMBOL_NAME", out symbolName) && !string.IsNullOrEmpty(symbolName) && symbolName.ToUpper() != "ANALYSIS_INCONCLUSIVE")
		{
			fromBangAnalyze = true;
		}
		stackFrames = GetFaultingStack(bangAnalyzeData, debugger.Is32Bit);
		if (stackFrames == null || stackFrames.Count == 0)
		{
			stackFrames = GetFaultingStack(debugger);
		}
		if (!fromBangAnalyze)
		{
			LoadIgnoreList();
			foreach (string stackFrame in stackFrames)
			{
				if (!IgnoreFrame(stackFrame))
				{
					symbolName = stackFrame;
					break;
				}
			}
		}
		string[] array = symbolName.Split('!');
		if (string.IsNullOrEmpty(moduleName))
		{
			moduleName = array[0];
		}
		if (array.Length > 1)
		{
			functionName = array[1].Split('+')[0];
		}
	}

	private void LoadIgnoreList()
	{
	}

	private bool IgnoreFrame(string frame)
	{
		if (!frame.Contains("!"))
		{
			return true;
		}
		if (ignoreFrames.Contains(frame))
		{
			return true;
		}
		foreach (string ignorePattern in ignorePatterns)
		{
			if (WildCardMatch(frame, ignorePattern))
			{
				return true;
			}
		}
		return false;
	}

	private bool WildCardMatch(string frame, string pattern)
	{
		return false;
	}

	private List<string> GetFaultingStack(NetDbgObj debugger)
	{
		debugger.SetContextFromTrapFrame();
		new List<string>();
		string stackText = debugger.Execute("kv");
		return GetStackFrames(debugger.Is32Bit, stackText, "\n");
	}

	private static List<string> GetStackFrames(bool is32Bit, string stackText, string delimiter)
	{
		if (string.IsNullOrEmpty(stackText))
		{
			return null;
		}
		List<string> list = new List<string>();
		string[] array = stackText.Split(new string[1] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(' ');
			if (is32Bit)
			{
				if (array2.Length >= 6 && array2[0].Length == 8 && array2[1].Length == 8 && array2[2].Length == 8 && array2[3].Length == 8 && array2[4].Length == 8)
				{
					list.Add(array2[5]);
				}
			}
			else if (array2.Length >= 9 && array2[0].Length == 17 && array2[1].Length == 17 && array2[2] == ":" && array2[3].Length == 17 && array2[4].Length == 17 && array2[5].Length == 17 && array2[6].Length == 17 && array2[7] == ":")
			{
				list.Add(array2[8]);
			}
		}
		return list;
	}

	private List<string> GetFaultingStack(Dictionary<string, string> bangAnalyzeData, bool is32Bit)
	{
		new List<string>();
		if (bangAnalyzeData.TryGetValue("STACK_TEXT", out var value))
		{
			return GetStackFrames(is32Bit, value, "\n");
		}
		return null;
	}

	private string GetBugCheckArgsTable(Dictionary<string, string> bangAnalyzeData, BugCheckData bugCheckData)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		string value = null;
		if (bangAnalyzeData.TryGetValue("Arguments", out value))
		{
			return $"<pre>{value}</pre>";
		}
		HTMLTable val = new HTMLTable();
		val.AddRow(new object[2] { "Arg1", bugCheckData.Arg1 });
		val.AddRow(new object[2] { "Arg2", bugCheckData.Arg2 });
		val.AddRow(new object[2] { "Arg3", bugCheckData.Arg3 });
		val.AddRow(new object[2] { "Arg4", bugCheckData.Arg4 });
		return ((object)val).ToString();
	}

	public bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason)
	{
		foreach (string dumpFile in manager.GetDumpFiles())
		{
			NetDbgObj val = NetDbgObj.OpenDump(dumpFile, (string)null, (string)null, (object)null, false, true, true);
			try
			{
				if (val.IsKernelMode)
				{
					return true;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		return false;
	}
}
