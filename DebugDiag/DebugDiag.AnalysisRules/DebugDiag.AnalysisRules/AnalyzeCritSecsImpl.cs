using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DebugDiag.DbgLib;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

internal class AnalyzeCritSecsImpl : IAnalyzeCritSecs
{
	private int THREADNUM_INVALID;

	private IDbgCritSec CritSec;

	public void AnalyzeRootLocks()
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Expected O, but got Unknown
		IDbgCritSec val = null;
		string text = "";
		double num = 0.0;
		Dictionary<double, IDbgCritSec> dictionary = null;
		Dictionary<double, IDbgCritSec> dictionary2 = null;
		Dictionary<string, double> dictionary3 = new Dictionary<string, double>();
		Dictionary<double, CacheFunctions.ScriptModuleClass> dictionary4 = null;
		int num2 = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass2 = null;
		string text2 = "";
		bool flag = false;
		bool flag2 = false;
		int num3 = 0;
		string text3 = "";
		string text4 = "";
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		bool bMixedComcallCritsecDeadlockDetected = false;
		int num7 = 0;
		if (Convert.ToInt32(Globals.g_Debugger.CritSecs.Count) == 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Searching for possible deadlocks, orphaned locks and lock convoys", Globals.g_Debugger.CritSecs.Count + Globals.g_ThreadInfoCache.Count, "Lock");
		num2 = 0;
		dictionary = new Dictionary<double, IDbgCritSec>();
		dictionary2 = new Dictionary<double, IDbgCritSec>();
		dictionary4 = new Dictionary<double, CacheFunctions.ScriptModuleClass>();
		flag2 = true;
		text3 = "(Critical Sections ";
		foreach (IDbgCritSec critSec in Globals.g_Debugger.CritSecs)
		{
			IDbgCritSec val2 = critSec;
			dictionary.Clear();
			dictionary3.Clear();
			dictionary4.Clear();
			if (Globals.g_collCritSecs.ContainsKey(val2.Address))
			{
				continue;
			}
			switch (Convert.ToString(val2.State).ToUpper())
			{
			case "DEADLOCKED":
			case "TRANSITIONING":
			case "ORPHANED":
			case "UNINITIALIZED":
			case "UNLOCKED":
				text2 = "Lock at " + Globals.HelperFunctions.GetCritSecWithLink(val2.Address) + " is <b>" + Convert.ToString(val2.State) + "</b>";
				if (Convert.ToString(val2.State).ToUpper() == "DEADLOCKED")
				{
					scriptThreadClass = Globals.g_ThreadInfoCache.Item(val2.OwnerThreadID);
					if (Convert.ToInt64(scriptThreadClass.WaitingOnCritSecAddr) != 0L)
					{
						val = Globals.g_Debugger.CritSecs.GetCritSecByAddress(scriptThreadClass.WaitingOnCritSecAddr);
						if (Convert.ToString(val.State).ToUpper() == "DEADLOCKED")
						{
							text2 = "Lock at " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(val2.Address)) + " owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(val2.OwnerThreadID)) + " is <b>Deadlocked</b> with lock at " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(val.Address)) + " owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(val.OwnerThreadID));
							if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collCritSecs.ContainsKey(val.Address))))
							{
								Globals.g_collCritSecs.Add(val.Address, val);
							}
							dictionary.Add(val.Address, val);
						}
					}
					else
					{
						text2 = text2 + " (owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(val2.OwnerThreadID)) + ")";
					}
					flag2 = false;
				}
				else if (Convert.ToString(val2.State).ToUpper() == "ORPHANED" || Convert.ToString(val2.State).ToUpper() == "UNINITIALIZED")
				{
					flag2 = false;
				}
				text2 += "<br>";
				dictionary.Add(val2.Address, val2);
				Globals.g_collCritSecs.Add(val2.Address, val2);
				break;
			default:
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(val2.OwnerThreadID);
				if (Convert.ToBoolean(IsCritSecBlockedByCOMCall(val2)))
				{
					num6 = Globals.g_ThreadInfoCache.ItemBySystemID(scriptThreadClass.COMDestinationThreadID)?.ThreadID ?? (-1);
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(val2.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(val2.OwnerThreadID, val2);
					}
					flag2 = false;
					dictionary.Add(val2.Address, val2);
					Globals.g_collCritSecs.Add(val2.Address, val2);
					text = Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(val2.Address));
					foreach (IDbgCritSec critSec2 in Globals.g_Debugger.CritSecs)
					{
						IDbgCritSec val3 = critSec2;
						if (val3.OwnerThreadID == val2.OwnerThreadID && val2.Address != val3.Address)
						{
							text = text + ", " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(val3.Address));
							if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collCritSecs.ContainsKey(val.Address))))
							{
								Globals.g_collCritSecs.Add(val3.Address, val3);
							}
						}
					}
					text2 = "<br>Lock(s) at " + text + " owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(val2.OwnerThreadID)) + " is/are <b>Deadlocked through a COM call</b> to thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(num6)) + " which is in turn waiting on a critical section owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(scriptThreadClass.ThreadID)) + ".<br>";
					Globals.g_MixedComcallCritsecDeadlockDetected = true;
					bMixedComcallCritsecDeadlockDetected = true;
				}
				else if (Convert.ToBoolean(IsCritSecBlocked(val2)))
				{
					dictionary2.Add(val2.Address, val2);
					Globals.g_collCritSecs.Add(val2.Address, val2);
				}
				break;
			}
			Globals.HelperFunctions.IncrementSubStatus();
			if (dictionary.Count <= 0)
			{
				continue;
			}
			text2 = ((!flag2) ? ("<b>Detected a serious critical section related problem </b> in " + Convert.ToString(Globals.g_ShortDumpFileName) + "<br>" + text2) : ("<b>Detected a possible critical section related problem </b> in " + Convert.ToString(Globals.g_ShortDumpFileName) + "<br>" + text2));
			text2 += "<br>Impact analysis<br><br>";
			if (dictionary2.Count > 0)
			{
				foreach (IDbgCritSec value in dictionary2.Values)
				{
					text3 = text3 + " " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(value.OwnerThreadID));
				}
				text3 += ")<br><br>";
				text2 = text2 + "<b>" + Convert.ToString(dictionary2.Count) + "</b> critical sections indirectly blocked<br><br>" + text3;
			}
			text4 = "(Threads ";
			num5 = 0;
			num2 = 0;
			num4 = 0;
			num3 = 500;
			for (num7 = 0; num7 <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num7++)
			{
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(num7);
				flag = false;
				if (Convert.ToBoolean(IsThreadBlockedByCritSec(scriptThreadClass, val2)))
				{
					flag = true;
					num = GetBlockingFunction(scriptThreadClass);
					if (Convert.ToDouble(num) != 0.0 && !dictionary3.ContainsKey(num.ToString()))
					{
						dictionary3.Add(Convert.ToString(num), num);
					}
				}
				if (flag)
				{
					num3++;
					text4 = text4 + " " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(scriptThreadClass.ThreadID));
					num2++;
					if (Globals.g_ASPInfo.GetASPRequestByThreadID(scriptThreadClass.ThreadID) != null)
					{
						num3 += 2;
						num4++;
					}
					else if (Globals.g_HTTPInfo.GetClientConnectionByThreadID(scriptThreadClass.ThreadID) != null)
					{
						num3 += 2;
						num5++;
					}
					if (Convert.ToBoolean(Convert.ToInt32(Globals.g_collThreadsBlockedByCritsecs.ContainsKey(scriptThreadClass.ThreadID))))
					{
						Globals.g_collThreadsBlockedByCritsecs.Add(scriptThreadClass.ThreadID, scriptThreadClass);
					}
				}
				Globals.HelperFunctions.IncrementSubStatus();
			}
			text4 += ")<br><br>";
			double number;
			if (num4 > 0)
			{
				number = (double)(num4 * 100) / Convert.ToDouble(Globals.g_ASPInfo.CurrentRequests.Count);
				number = Globals.HelperFunctions.FormatNumber(number, 2);
				text2 = text2 + "<b>" + Convert.ToString(number) + "%</b> of executing ASP Requests blocked<br><br>";
			}
			if (num5 > 0)
			{
				number = (double)(num5 * 100) / Convert.ToDouble(Globals.g_HTTPInfo.ATQThreadCount);
				number = Globals.HelperFunctions.FormatNumber(number, 2);
				text2 = text2 + "<b>" + Convert.ToString(number) + "%</b> of IIS worker threads blocked<br><br>";
			}
			number = (double)(num2 * 100) / Convert.ToDouble(Globals.g_ThreadInfoCache.Count);
			number = Globals.HelperFunctions.FormatNumber(number, 2);
			text2 = text2 + "<b>" + Convert.ToString(number) + "%</b> of threads blocked<br><br>" + text4;
			if (dictionary3.Count > 0)
			{
				text2 += "The following functions are involved in the root cause<br>";
				foreach (KeyValuePair<string, double> item in dictionary3)
				{
					text2 = text2 + "<b>" + Convert.ToString(CacheFunctions.GetSymbolFromAddress(item.Value)) + "</b><br>";
					scriptModuleClass = CacheFunctions.GetModuleFromAddress(item.Value);
					if (scriptModuleClass != null && !dictionary4.ContainsKey(scriptModuleClass.Base))
					{
						dictionary4.Add(scriptModuleClass.Base, scriptModuleClass);
					}
				}
			}
			if (dictionary4.Count > 0)
			{
				text2 += "<br>The following modules are involved in the root cause<br>";
				foreach (CacheFunctions.ScriptModuleClass value2 in dictionary4.Values)
				{
					text2 += Convert.ToString(value2.ImageName);
					if (Convert.ToString(value2.VSCompanyName) != "")
					{
						text2 = text2 + " from <b>" + Convert.ToString(scriptModuleClass.VSCompanyName) + "</b>";
					}
					text2 += "<br>";
				}
			}
			ReportCritSecProblem(dictionary, dictionary3, dictionary4, flag2, bMixedComcallCritsecDeadlockDetected, text2, num3);
		}
	}

	private void ReportCritSecProblem(Dictionary<double, IDbgCritSec> CritSecs, Dictionary<string, double> BlockingFunctions, Dictionary<double, CacheFunctions.ScriptModuleClass> BlockingModules, bool bLockConvoyOnly, bool bMixedComcallCritsecDeadlockDetected, string Description, int Weight)
	{
		string empty = string.Empty;
		string SolutionSourceID = "";
		bool bGenericCritsecRecommendation = false;
		empty = GetRecommendationForCritSecProblem(CritSecs, BlockingFunctions, BlockingModules, bLockConvoyOnly, ref bGenericCritsecRecommendation, bMixedComcallCritsecDeadlockDetected, ref SolutionSourceID);
		if (SolutionSourceID == "")
		{
			SolutionSourceID = "{b7dd18f2-28ff-4e19-ab39-c41e7f050438}";
		}
		if (Convert.ToBoolean(bGenericCritsecRecommendation) && !CritSecs.Any((KeyValuePair<double, IDbgCritSec> kvp) => kvp.Value.State.ToUpper() == "DEADLOCKED"))
		{
			Globals.Manager.ReportWarning(Description, empty, Weight, SolutionSourceID);
			return;
		}
		Globals.Manager.ReportError(Description, empty, Weight, SolutionSourceID);
		Globals.g_IsBlockingIssueDetected = true;
	}

	public void AnalyzeCritSecs()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		if (Convert.ToInt32(Globals.g_Debugger.CritSecs.Count) == 0)
		{
			return;
		}
		foreach (IDbgCritSec critSec in Globals.g_Debugger.CritSecs)
		{
			IDbgCritSec val = critSec;
			if (!Globals.g_collCritSecs.ContainsKey(val.Address))
			{
				AnalyzeCritSec(val);
				Globals.g_collCritSecs.Add(val.Address, val);
			}
		}
	}

	public void ReportCritSecs()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		if (Convert.ToInt32(Globals.g_Debugger.CritSecs.Count) == 0)
		{
			return;
		}
		ReportSection currentSection = Globals.Manager.CurrentSection;
		ReportSection val = currentSection.AddChildSection("CritSecReport", (SectionType)0);
		val.Title = "Locked critical section report";
		Globals.Manager.CurrentSection = val;
		foreach (IDbgCritSec critSec2 in Globals.g_Debugger.CritSecs)
		{
			IDbgCritSec critSec = critSec2;
			ReportCritSec(critSec);
		}
		Globals.Manager.CurrentSection = currentSection;
	}

	private void AnalyzeCritSec(IDbgCritSec CritSec)
	{
		double num = 0.0;
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		Dictionary<double, CacheFunctions.ScriptModuleClass> dictionary2 = new Dictionary<double, CacheFunctions.ScriptModuleClass>();
		int num2 = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		string text = "";
		int num3 = 0;
		string text2 = "";
		string text3 = "";
		int num4 = 0;
		int num5 = 0;
		Dictionary<double, IDbgCritSec> dictionary3 = new Dictionary<double, IDbgCritSec>();
		string empty = string.Empty;
		string[] array = null;
		int num6 = 0;
		scriptThreadClass = Globals.g_ThreadInfoCache.Item(CritSec.OwnerThreadID);
		if (IsThreadBlockedByAnotherCritSec(scriptThreadClass))
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Analyzing critical section " + Convert.ToString(CacheFunctions.GetSymbolFromAddress(CritSec.Address)), Globals.g_ThreadInfoCache.Count, "Thread");
		num3 = 0;
		num2 = 0;
		HashSet<double> hashSet = new HashSet<double>();
		text2 = "(Critical Sections ";
		text3 = "(Threads";
		for (num6 = 0; num6 <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num6++)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(num6);
			if (IsThreadBlockedByCritSec(scriptThreadClass, CritSec))
			{
				num3++;
				text3 = text3 + " " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(scriptThreadClass.ThreadID));
				Globals.g_collThreadsBlockedByCritsecs.Add(scriptThreadClass.ThreadID, scriptThreadClass);
				num2++;
				if (scriptThreadClass.WaitingOnCritSecAddr == CritSec.Address)
				{
					num = GetBlockingFunction(scriptThreadClass);
					if (!dictionary.ContainsKey(Convert.ToString(num)))
					{
						dictionary.Add(Convert.ToString(num), num);
					}
					if (Globals.g_ASPInfo.GetASPRequestByThreadID(scriptThreadClass.ThreadID) != null)
					{
						num3 += 2;
						num4++;
					}
					else if (Globals.g_HTTPInfo.GetClientConnectionByThreadID(scriptThreadClass.ThreadID) != null)
					{
						num3 += 2;
						num5++;
					}
				}
				else if (Convert.ToInt64(scriptThreadClass.WaitingOnCritSecAddr) != 0L && !hashSet.Contains(scriptThreadClass.WaitingOnCritSecAddr))
				{
					hashSet.Add(scriptThreadClass.WaitingOnCritSecAddr);
				}
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		text3 += ")<br><br>";
		if (num2 <= 0)
		{
			return;
		}
		text = "<b>Detected possible blocking or leaked critical section at " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(CritSec.Address)) + " owned by thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(CritSec.OwnerThreadID)) + "</b> in " + Convert.ToString(Globals.g_ShortDumpFileName) + "<br><br>Impact of this lock<br><br>";
		if (hashSet.Count > 0)
		{
			foreach (double item in hashSet)
			{
				text2 = text2 + " " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(item));
			}
			text2 += ")<br><br>";
			text = text + "<b>" + Convert.ToString(hashSet.Count) + "</b> critical sections indirectly blocked<br><br>" + text2;
		}
		double number;
		if (num4 > 0)
		{
			number = (double)(num4 * 100) / Convert.ToDouble(Globals.g_ASPInfo.CurrentRequests.Count);
			number = Globals.HelperFunctions.FormatNumber(number, 2);
			text = text + "<b>" + Convert.ToString(number) + "%</b> of executing ASP Requests blocked<br><br>";
		}
		if (num5 > 0)
		{
			number = (double)(num5 * 100) / Convert.ToDouble(Globals.g_HTTPInfo.ATQThreadCount);
			number = Globals.HelperFunctions.FormatNumber(number, 2);
			text = text + "<b>" + Convert.ToString(number) + "%</b> of IIS worker threads blocked<br><br>";
		}
		number = (double)(num2 * 100) / Convert.ToDouble(Globals.g_ThreadInfoCache.Count);
		number = Globals.HelperFunctions.FormatNumber(number, 2);
		text = text + "<b>" + Convert.ToString(number) + "%</b> of threads blocked<br><br>" + text3;
		if (dictionary.Count > 0)
		{
			text += "The following functions are trying to enter this critical section<br>";
			foreach (KeyValuePair<string, double> item2 in dictionary)
			{
				text = text + "<b>" + Convert.ToString(CacheFunctions.GetSymbolFromAddress(item2.Value)) + "</b><br>";
				scriptModuleClass = CacheFunctions.GetModuleFromAddress(item2.Value);
				if (scriptModuleClass != null && !dictionary2.ContainsKey(scriptModuleClass.Base))
				{
					dictionary2.Add(scriptModuleClass.Base, scriptModuleClass);
				}
			}
		}
		if (dictionary2.Count > 0)
		{
			text += "<br>The following module(s) are involved with this critical section<br>";
			foreach (CacheFunctions.ScriptModuleClass value in dictionary2.Values)
			{
				text += Convert.ToString(value.ImageName);
				if (Convert.ToString(value.VSCompanyName) != "")
				{
					text = text + " from <b>" + Convert.ToString(value.VSCompanyName) + "</b>";
				}
				text += "<br>";
			}
		}
		empty = Globals.HelperFunctions.CheckSymbolType(CritSec.Address);
		array = Globals.HelperFunctions.Split(empty.ToString(), ":");
		if (array[2] == "0" && array[0] != "UNKNOWN_MODULE")
		{
			text = "<b>WARNING</b> - DebugDiag was unable to locate debug symbols for <b>" + array[0] + "</b>, so the information below may be incomplete.<br><br>" + text;
		}
		dictionary3.Add(CritSec.Address, CritSec);
		ReportCritSecProblem(dictionary3, dictionary, dictionary2, bLockConvoyOnly: false, bMixedComcallCritsecDeadlockDetected: false, text, num3);
	}

	private bool IsCritSecBlockedByCOMCall(IDbgCritSec CritSec)
	{
		bool result = false;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass2 = null;
		IDbgCritSec val = null;
		scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID((int)CritSec.OwnerThreadSystemID);
		if (scriptThreadClass == null)
		{
			return result;
		}
		if (Convert.ToInt64(scriptThreadClass.WaitingOnCritSecAddr) == 0L && Globals.g_Debugger.ProcessID == scriptThreadClass.COMDestinationProcessID && Convert.ToInt32(scriptThreadClass.COMDestinationThreadID) != 0)
		{
			scriptThreadClass2 = Globals.g_ThreadInfoCache.ItemBySystemID(scriptThreadClass.COMDestinationThreadID);
			if (scriptThreadClass2 != null && Convert.ToInt64(scriptThreadClass2.WaitingOnCritSecAddr) != 0L)
			{
				val = Globals.g_Debugger.CritSecs.GetCritSecByAddress(scriptThreadClass2.WaitingOnCritSecAddr);
				if (val != null && val.OwnerThreadID == scriptThreadClass.ThreadID)
				{
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collDeadLockedThreads.ContainsKey(val.OwnerThreadID))))
					{
						Globals.g_collDeadLockedThreads.Add(scriptThreadClass2.ThreadID, scriptThreadClass);
					}
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collDeadLockedThreads.ContainsKey(scriptThreadClass.ThreadID))))
					{
						Globals.g_collDeadLockedThreads.Add(scriptThreadClass.ThreadID, scriptThreadClass);
					}
					return true;
				}
			}
		}
		return false;
	}

	private bool IsThreadBlocked(CacheFunctions.ScriptThreadClass Thread)
	{
		bool result = false;
		IDbgCritSec val = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		if (Convert.ToInt64(Thread.WaitingOnCritSecAddr) != 0L)
		{
			val = Globals.g_Debugger.CritSecs.GetCritSecByAddress(Thread.WaitingOnCritSecAddr);
			result = IsCritSecBlocked(val);
		}
		else if (Globals.g_Debugger.ProcessID == Thread.COMDestinationProcessID && Convert.ToInt32(Thread.COMDestinationThreadID) != 0 && Thread.COMDestinationThreadID != THREADNUM_INVALID)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(Thread.COMDestinationThreadID);
			if (scriptThreadClass != null)
			{
				result = IsThreadBlocked(scriptThreadClass);
			}
		}
		return result;
	}

	private bool IsCritSecBlocked(IDbgCritSec CritSec)
	{
		bool flag = false;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		string text = "";
		switch (Convert.ToString(CritSec.State).ToUpper())
		{
		case "LOCKED":
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(CritSec.OwnerThreadID);
			return IsThreadBlocked(scriptThreadClass);
		case "DEADLOCKED":
		case "TRANSITIONING":
		case "ORPHANED":
		case "UNINITIALIZED":
		case "UNLOCKED":
			return true;
		default:
			return false;
		}
	}

	private bool IsThreadBlockedByCritSec(CacheFunctions.ScriptThreadClass Thread, IDbgCritSec CritSec)
	{
		Globals.g_collThreadsBlockedByThisCritsec.Clear();
		return IsThreadBlockedByCritSec_Recurse(Thread, CritSec);
	}

	private bool IsThreadBlockedByCritSec_Recurse(CacheFunctions.ScriptThreadClass Thread, IDbgCritSec CritSec)
	{
		bool result = false;
		IDbgCritSec val = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		if (Convert.ToBoolean(Globals.g_collThreadsBlockedByThisCritsec.ContainsKey(Thread.ThreadID)))
		{
			return false;
		}
		Globals.g_collThreadsBlockedByThisCritsec.Add(Thread.ThreadID, Thread.ThreadID);
		if (Thread.WaitingOnCritSecAddr == CritSec.Address || Convert.ToBoolean(Globals.g_collDeadLockedThreads.ContainsKey(Thread.ThreadID)))
		{
			result = true;
		}
		else if (Convert.ToInt64(Thread.WaitingOnCritSecAddr) != 0L)
		{
			val = Globals.g_Debugger.CritSecs.GetCritSecByAddress(Thread.WaitingOnCritSecAddr);
			if (Convert.ToString(val.State).ToUpper() == "LOCKED")
			{
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(val.OwnerThreadID);
				result = IsThreadBlockedByCritSec_Recurse(scriptThreadClass, CritSec);
			}
		}
		else if (Globals.g_Debugger.ProcessID == Thread.COMDestinationProcessID && Convert.ToInt32(Thread.COMDestinationThreadID) != 0 && Thread.COMDestinationThreadID != THREADNUM_INVALID)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(Thread.COMDestinationThreadID);
			if (scriptThreadClass != null)
			{
				result = IsThreadBlockedByCritSec_Recurse(scriptThreadClass, CritSec);
			}
		}
		return result;
	}

	private bool IsThreadBlockedByAnotherCritSec(CacheFunctions.ScriptThreadClass Thread)
	{
		bool result = false;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		if (Convert.ToInt64(Thread.WaitingOnCritSecAddr) != 0L)
		{
			result = true;
		}
		else if (Globals.g_Debugger.ProcessID == Thread.COMDestinationProcessID && Convert.ToInt32(Thread.COMDestinationThreadID) != 0 && Thread.COMDestinationThreadID != THREADNUM_INVALID)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(Thread.COMDestinationThreadID);
			if (scriptThreadClass != null)
			{
				result = IsThreadBlockedByAnotherCritSec(scriptThreadClass);
			}
		}
		return result;
	}

	private double GetBlockingFunction(CacheFunctions.ScriptThreadClass Thread)
	{
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		int num = 0;
		for (num = 0; num <= Convert.ToInt32(Thread.StackFrames.Count) - 1; num++)
		{
			scriptStackFrameClass = Thread.StackFrames[num];
			if (scriptStackFrameClass.GetFrameText().ToUpper().IndexOf("NTDLL!RTLENTERCRITICALSECTION", 0) != -1)
			{
				return Convert.ToUInt64(scriptStackFrameClass.ReturnAddress);
			}
		}
		return 0.0;
	}

	private void ReportCritSec(IDbgCritSec CritSec)
	{
		Globals.Manager.Write("    <table border=0 cellpadding=0 cellspacing=0 class=myCustomText ID=\"Table1\">\r\n    <tr><td>Critical Section</td>\r\n        <td>&nbsp;&nbsp;<b>\r\n            <a name='" + Globals.g_UniqueReference + "CritSec" + Convert.ToString(CritSec.Address) + "'>" + CacheFunctions.GetSymbolFromAddress(CritSec.Address) + "</a></b></td>\r\n</tr>\r\n    <tr><td>Lock State</td><td>&nbsp;&nbsp;<b>" + CritSec.State + "</b></td></tr>\r\n    <tr><td>Lock Count</td><td>&nbsp;&nbsp;" + CritSec.LockCount + "</td></tr>\r\n    <tr><td>Recursion Count</td><td>&nbsp;&nbsp;" + CritSec.RecursionCount + "</td></tr>\r\n    <tr><td>Entry Count</td><td>&nbsp;&nbsp;" + CritSec.EntryCount + "</td></tr>\r\n    <tr><td>Contention Count</td><td>&nbsp;&nbsp;" + CritSec.ContentionCount + "</td></tr>\r\n    <tr><td>Spin Count</td><td>&nbsp;&nbsp;" + CritSec.SpinCount + "</td></tr>\r\n");
		switch (Convert.ToString(CritSec.State).ToUpper())
		{
		case "LOCKED":
		case "DEADLOCKED":
			Globals.Manager.Write("            <tr><td>Owner Thread</td><td>&nbsp;&nbsp;" + Globals.HelperFunctions.GetThreadIDWithLink(CritSec.OwnerThreadID).ToString() + "</td></tr>\r\n            <tr><td>Owner Thread System ID</td><td>&nbsp;&nbsp;<b>" + CritSec.OwnerThreadSystemID + "</b></td></tr>\r\n");
			break;
		case "ORPHANED":
		case "UNINITIALIZED":
			Globals.Manager.Write("            <tr><td>Owner Thread System ID</td><td>&nbsp;&nbsp;<b>" + CritSec.OwnerThreadSystemID + " (not present in dump)</b></td></tr>\r\n");
			break;
		}
		Globals.Manager.Write("    </table><br><br>\r\n");
	}

	private string GetRecommendationForCritSecProblem(Dictionary<double, IDbgCritSec> CritSecs, Dictionary<string, double> BlockingFunctions, Dictionary<double, CacheFunctions.ScriptModuleClass> BlockingModules, bool IsLockConvoy, ref bool bGenericCritsecRecommendation, bool bMixedComcallCritsecDeadlockDetected, ref string SolutionSourceID)
	{
		IDbgCritSec val = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		string[] array = null;
		string text = "";
		Dictionary<string, CacheFunctions.ScriptModuleClass> dictionary = null;
		bool bGenericLoaderLockRecommendationGiven = false;
		object obj = null;
		Dictionary<int, string> colFrameTexts = new Dictionary<int, string>();
		Dictionary<int, string> colFrameTexts2 = new Dictionary<int, string>();
		if (Convert.ToBoolean(~Convert.ToInt32(IsLockConvoy)))
		{
			foreach (KeyValuePair<string, double> BlockingFunction in BlockingFunctions)
			{
				if (Convert.ToString(CacheFunctions.GetSymbolFromAddress(BlockingFunction.Value)).ToUpper().IndexOf("HEAP", 0) != -1)
				{
					text = "One or more of the blocking functions is a heap function. In most cases this implies  leaked critical section caused by <b>heap corruption</b>. Please follow the steps outlined in the following Knowledge Base article: <br> <a target='_blank' href='http://support.microsoft.com/?id=300966'> 300966 Howto debug heap corruption issues in Internet Information Services (IIS) </a>";
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401a8}";
					return text;
				}
			}
		}
		foreach (IDbgCritSec value2 in CritSecs.Values)
		{
			array = Globals.HelperFunctions.Split(CacheFunctions.GetSymbolFromAddress(value2.Address), "+");
			switch (array[0])
			{
			case "KERNEL32!GCSNLSPROCESSCACHE":
				text = "<b>The lock convoy identified is a known issue.</b> Please follow the steps in the following Knowledge Base article: <br> <a target='_blank' href='http://support.microsoft.com/?id=830852'> 830852 - Poor performance in multi-threaded applications that call string compare functions </a>";
				if (!Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))
				{
					Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
				}
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401a9}";
				return text;
			case "INFOCOMM!IIS_SERVICE::SM_CSLOCK":
				text = "<b>INFOCOMM is currently holding a critical section lock opening up a socket.</b> This is known issue. Please follow the steps in the following Knowledge Base article: <br> <a target='_blank' href='http://support.microsoft.com/?id=328512'> 328512 IIS Stops Responding After You Apply Updates</a>";
				if (!Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))
				{
					Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
				}
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401aa}";
				return text;
			case "ASP!G_TEMPLATECACHE":
				text = "ASP.DLL is currently holding a Critical Section Lock on ASP template cache manager, which can result in poor performance, depending on the number of ASP templates currently cached. Please review the <a href='#ASPReport" + Globals.g_UniqueReference + "'>ASP details</a> section of this report and check the value for <b>ASP Templates Cached</b>.<br><br>If you are hitting the limit for the maximum number of ASP templates IIS is configured to cache, then moving a few Session-Independent applications into their own processes (Isolation) may help to improve performance. Further information on how to optimize performance in IIS can be found on the <a target='_blank' href='http://www.microsoft.com/iis'>IIS Portal</a>, or by contacting Microsoft Corporation.";
				if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))))
				{
					Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
				}
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401ab}";
				return text;
			case "MSJTES40!__SMALL_BLOCK_HEAP":
			case "MSJTES40!G_CRITICALSECTION":
			case "MSJTES40!DLLREGISTERSERVER":
				if (Globals.g_OSVER == Globals.OS_VER_WIN2K3)
				{
					text = "This may be caused by a known issue in the JET database engine. Please see the following Knowledge Base article for more information on how to obtain the hotfix: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=838306'>838306 - FIX: Web applications that use the Jet database engine may stop responding under heavy load </a></b>";
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
					}
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401ac}";
					return text;
				}
				break;
			case "MSVBVM60!RBY_THREADPOOL":
				if (Convert.ToInt32(value2.OwnerThreadID) >= 0)
				{
					text = "Thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(value2.OwnerThreadID)) + " appears to be deadlocked attempting to unload the following VB module while other threads are trying to load it:<br><br><b>" + Convert.ToString(AnalyzeVBModInfo.AnalyzeRimUeIssue((int)value2.OwnerThreadSystemID)) + "</b><br><br>This problem is usually caused by a VB module not being compiled with the 'Retain in Memory' and 'Unattended Execution' options enabled. Please see the recommendation below regarding miscompiled VB modules to resolve this issue.";
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
					}
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401ad}";
					return text;
				}
				break;
			case "ASP!G_DIRMONITOR":
				if (Convert.ToInt32(value2.OwnerThreadID) >= 0 && Convert.ToBoolean(IsAspAppRestarting(scriptThreadClass)))
				{
					text = "An ASP application is currently in the middle of an application restart, which will result in all requests to this application remaining blocked until the restart is complete.<br><br>ASP application restarts generally occur when IIS detects a change to the global.asa file for a particular application. This can be caused by several things, including backup software and anti-virus software. Please see the following KB article for more information: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=248013'>Q248013 - Err Msg: HTTP Error 500-12 Application Restarting</a></b> <br>";
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(val.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(val.OwnerThreadID, val);
					}
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401ae}";
					return text;
				}
				break;
			case "ASPNET_ISAPI!G_APPDOMAINLOCK":
				text = "This is likely caused by a known issue in the .NET Framework. Please see the following Knowledge Base article for more information on the problem, and how to obtain the hotfix: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=840512'>840512 - FIX: ASP.NET may stop responding, and the Aspnet_wp.exe process may become unresponsive or deadlock </a></b>";
				if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(val.OwnerThreadID))))
				{
					Globals.g_collKnownCSIssueFound.Add(val.OwnerThreadID, val);
				}
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401af}";
				return text;
			case "OLE32!GCOMLOCK":
			case "OLE32!GIPIDLOCK":
			case "OLE32!GCONTEXTLOCK":
			case "COMBASE!GCOMLOCK":
			case "COMBASE!GIPIDLOCK":
			case "COMBASE!GCONTEXTLOCK":
				if (Convert.ToBoolean(~Convert.ToInt32(IsLockConvoy)))
				{
					text = string.Concat("In most cases issues involving ", array, " are due to an earlier exception causing the critical section to be leaked.");
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
					}
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401b0}";
					return Convert.ToString(BuildRecommendationForCritSecProblem(text, value2.OwnerThreadID, array[0], false));
				}
				break;
			case "NTDLL!LDRPLOADERLOCK":
				if (Convert.ToInt32(value2.OwnerThreadID) < 0)
				{
					break;
				}
				text = Convert.ToString(GetRecommendationForLoaderLockIssue(scriptThreadClass, ref bGenericLoaderLockRecommendationGiven));
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401b1}";
				if (!Convert.ToBoolean(bGenericLoaderLockRecommendationGiven))
				{
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(value2.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(value2.OwnerThreadID, value2);
					}
					return text;
				}
				break;
			}
		}
		if (Convert.ToBoolean(bMixedComcallCritsecDeadlockDetected) && Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("SYSTEM_ENTERPRISESERVICES")))
		{
			colFrameTexts.Add(1, "SYSTEM_ENTERPRISESERVICES!SYSTEM.ENTERPRISESERVICES.SERVICEDCOMPONENTPROXY.SENDDESTRUCTIONEVENTS");
			colFrameTexts.Add(2, "SYSTEM_ENTERPRISESERVICES!SYSTEM.ENTERPRISESERVICES.SERVICEDCOMPONENTPROXY.CLEANUPQUEUES");
			colFrameTexts.Add(3, "SYSTEM_ENTERPRISESERVICES!SYSTEM.ENTERPRISESERVICES.SERVICEDCOMPONENTPROXYATTRIBUTE.SYSTEM.RUNTIME.INTEROPSERVICES.ICUSTOMFACTORY.CREATEINSTANCE");
			colFrameTexts2.Add(1, "SYSTEM.ENTERPRISESERVICES.THUNK.PROXY.SENDDESTRUCTIONEVENTS");
			colFrameTexts2.Add(2, "SYSTEM.ENTERPRISESERVICES.SERVICEDCOMPONENTPROXY.CLEANUPQUEUES");
			colFrameTexts2.Add(3, "SYSTEM.ENTERPRISESERVICES.SERVICEDCOMPONENTPROXYATTRIBUTE.SYSTEM.RUNTIME.INTEROPSERVICES.ICUSTOMFACTORY.CREATEINSTANCE");
			foreach (double key in CritSecs.Keys)
			{
				val = CritSecs[key];
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(val.OwnerThreadID);
				if (scriptThreadClass != null)
				{
					bool value = scriptThreadClass.FindFramesInStackInOrder(ref colFrameTexts);
					if (Convert.ToBoolean(~Convert.ToInt32(value)))
					{
						value = scriptThreadClass.FindFramesInStackInOrder(ref colFrameTexts2);
					}
					if (Convert.ToBoolean(value))
					{
						SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401b2}";
						return "To resolve this deadlock, the client application must call the <b>.Dispose</b> method on all <b>System.EnterpriseServices.ServicedComponent</b> instances.  See the following articles for more information:<ul><li>" + Convert.ToString(Globals.HelperFunctions.Spaces(5) + "<a href=http://www.gotdotnet.com/team/xmlentsvcs/esfaq.aspx#1.1>Enterprise Services FAQ</a></li><li>" + Convert.ToString(Globals.HelperFunctions.Spaces(5) + "<a target='_blank' href='http://support.microsoft.com/?id=298014'>Microsoft Knowledge Base Article 298014</a></li></ul>"));
					}
				}
			}
		}
		foreach (double key2 in CritSecs.Keys)
		{
			val = CritSecs[key2];
			array = Globals.HelperFunctions.Split(CacheFunctions.GetSymbolFromAddress(val.Address), "+");
			if (Convert.ToString(val.State).ToUpper() == "ORPHANED")
			{
				text = "The critical section at " + Convert.ToString(Globals.HelperFunctions.GetCritSecWithLink(val.Address)) + " has been orphaned, which means the owning thread is no longer present in the process.  In most cases this is due to an <b>earlier exception</b> causing  the thread to terminate without leaving (unlocking) the critical section.";
				SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401b3}";
				return Convert.ToString(BuildRecommendationForCritSecProblem(text, val.OwnerThreadID, array[0], true));
			}
		}
		foreach (double key3 in CritSecs.Keys)
		{
			val = CritSecs[key3];
			array = Globals.HelperFunctions.Split(CacheFunctions.GetSymbolFromAddress(val.Address), "+");
			obj = Globals.HelperFunctions.CheckSymbolType(val.Address);
			if (Globals.HelperFunctions.Split(obj.ToString(), ":")[0] == "UNKNOWN_MODULE")
			{
				text = Convert.ToString(ScanUnresolvedBlockingCritSec(val, ref SolutionSourceID));
				if (text != "")
				{
					SolutionSourceID = "{faf219a1-ad64-42b6-ae2e-704c3f0401b4}";
					return text;
				}
			}
		}
		bGenericCritsecRecommendation = true;
		if (text == "")
		{
			text = "The following vendors were identified for follow up based on root cause analysis<br><br>";
			dictionary = new Dictionary<string, CacheFunctions.ScriptModuleClass>();
			foreach (double key4 in BlockingModules.Keys)
			{
				CacheFunctions.ScriptModuleClass scriptModuleClass = BlockingModules[key4];
				if (!dictionary.ContainsKey(Convert.ToString(scriptModuleClass.VSCompanyName)) && Convert.ToString(scriptModuleClass.VSCompanyName) != "")
				{
					text = text + "<b>" + Convert.ToString(scriptModuleClass.VSCompanyName) + "</b><br>";
					dictionary.Add(Convert.ToString(scriptModuleClass.VSCompanyName), scriptModuleClass);
				}
				else if (Convert.ToString(scriptModuleClass.VSCompanyName) == "")
				{
					text = text + "<b>Unknown vendor for module " + Convert.ToString(scriptModuleClass.ImageName) + "</b><br>";
				}
			}
			text += "Please follow up with the vendors identified above";
		}
		return Convert.ToString(BuildRecommendationForCritSecProblem(text, val.OwnerThreadID, array[0], false));
	}

	private bool IsAspAppRestarting(CacheFunctions.ScriptThreadClass Thread)
	{
		throw new NotImplementedException();
	}

	private string BuildRecommendationForCritSecProblem(string sBaseRecommendation, int nThreadNumForExceptionScanning, string sModuleNameForExceptionScanning, object bAddAppVerifBlurb)
	{
		object obj = null;
		string text = "";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(sBaseRecommendation);
		if (Convert.ToBoolean(bAddAppVerifBlurb))
		{
			text = Convert.ToString(Globals.g_Debugger.ExecutableName);
			text = Path.GetFileName(text);
			stringBuilder.Append(" Consider the following approach to determine root cause for this critical section problem:<ol><li>Use a DebugDiag crash rule to monitor the application for exceptions which cause the owing thread of the critical section to exit prematurely</li><li>If there are no such exceptions, enable 'lock checks' in Application Verifier<ul><li>Download Application Verifier from the following URL:<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + "<a href='http://www.microsoft.com/downloads/details.aspx?FamilyID=bd02c19c-1250-433c-8c1b-2619bd93b3a2&displaylang=en'>Microsoft Application Verifier</a></li><li>Enable 'lock checks' for this process by running the following command:<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + "Appverif.exe -enable locks -for " + text + "</li><li>See the following document for more information on Application Verifier:<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + "<a href='http://msdn.microsoft.com/library/default.aspx?url=/library/en-us/dnappcom/html/appverifier.aspx?frame=true'>Testing Applications with AppVerifier</a></li></ul></li></ol>");
		}
		if (nThreadNumForExceptionScanning == THREADNUM_INVALID)
		{
			obj = GetPreviousExceptionsRecommendationStringForOrphanedLock(sModuleNameForExceptionScanning);
		}
		else if (Convert.ToInt32(nThreadNumForExceptionScanning) >= 0)
		{
			obj = GetPreviousExceptionsRecommendationStringForOwnedLock(nThreadNumForExceptionScanning.ToString(), sModuleNameForExceptionScanning);
		}
		if (Convert.ToString(obj) != "")
		{
			stringBuilder.Append(Convert.ToString(obj) ?? "");
		}
		return stringBuilder.ToString();
	}

	private string ScanUnresolvedBlockingCritSec(IDbgCritSec CritSec, ref string SolutionSourceID)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		int num = 0;
		int num2 = 0;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		string text = "";
		string text2 = "";
		foreach (CacheFunctions.ScriptThreadClass value in Globals.g_collThreadsBlockedByCritsecs.Values)
		{
			for (num = 0; num <= Convert.ToInt32(value.StackFrames.Count) - 1; num++)
			{
				scriptStackFrameClass = value.StackFrames[num];
				switch (Convert.ToString(CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress)))
				{
				case "W3ISAPI!ISAPI_DLL::LOAD":
				{
					scriptStackFrameClass = value.StackFrames[num + 1];
					string result = "One or more of the blocked threads are waiting on the following ISAPI handler:<br><br><b>" + Convert.ToString(Globals.g_Debugger.ReadUnicodeString(scriptStackFrameClass.Args(0))) + "</b>.<br><br>Please follow up with the vendor of this module for further assistance on this issue.";
					if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(CritSec.OwnerThreadID))))
					{
						Globals.g_collKnownCSIssueFound.Add(CritSec.OwnerThreadID, CritSec);
					}
					SolutionSourceID = "{d0e43a48-c97b-4f16-a7ac-6372ba844414}";
					return result;
				}
				case "MSJET40!UTILENTERCRITICALSECTION":
					if (Globals.g_OSVER == Globals.OS_VER_WIN2K3)
					{
						if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(CritSec.OwnerThreadID))))
						{
							Globals.g_collKnownCSIssueFound.Add(CritSec.OwnerThreadID, CritSec);
						}
						SolutionSourceID = "{d0e43a48-c97b-4f16-a7ac-6372ba844415}";
						return "This may be caused by a known issue in the JET database engine. Please see the following Knowledge Base article for more information on how to obtain the hotfix: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=838306'>838306 - FIX: Web applications that use the Jet database engine may stop responding under heavy load </a></b>";
					}
					break;
				case "NTDLL!RTLDEBUGALLOCATEHEAP":
					if (Convert.ToBoolean(Globals.g_LeakTrackInfo.IsLeakTrackLoaded) && Convert.ToInt32(Globals.g_GlobalFlagsValue) != 0)
					{
						string result2 = "This issue may be due to pageheap being enabled in the process while LeakTrack is injected. It is recommended that any globalflags currently set for this process be disabled before reinjecting LeakTrack into the process.Information on how to disable pageheap can be found in the following KB article:<br><br> <a target='_blank' href='http://support.microsoft.com/?id=300966'> 300966 Howto debug heap corruption issues in Internet Information Services (IIS) </a><br><br><b>Current NTGlobalFlags value: 0x" + Convert.ToInt32(Globals.g_GlobalFlagsValue).ToString("X") + "</b>";
						if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(CritSec.OwnerThreadID))))
						{
							Globals.g_collKnownCSIssueFound.Add(CritSec.OwnerThreadID, CritSec);
						}
						SolutionSourceID = "{d0e43a48-c97b-4f16-a7ac-6372ba844416}";
						return result2;
					}
					break;
				case "WAM!SE_TABLE::GETEXTENSION":
					if (Globals.g_OSVER > Globals.OS_VER_WIN2K)
					{
						break;
					}
					scriptThreadClass = Globals.g_ThreadInfoCache.Item(CritSec.OwnerThreadID);
					for (num2 = 0; num2 <= Convert.ToInt32(scriptThreadClass.StackFrames.Count) - 1; num2++)
					{
						text2 = Convert.ToString(CacheFunctions.GetFunctionName(scriptThreadClass.StackFrames[num2].InstructionAddress));
						if (text2 == "RPCRT4!UTIL_WAITFORSYNCIO")
						{
							if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collKnownCSIssueFound.ContainsKey(CritSec.OwnerThreadID))))
							{
								Globals.g_collKnownCSIssueFound.Add(CritSec.OwnerThreadID, CritSec);
							}
							SolutionSourceID = "{d0e43a48-c97b-4f16-a7ac-6372ba844417}";
							return "This hang was likely caused by a known issue in WMI. Please see the following Knowledge Base article for further information, and instrucitons on how to obtain the hotfix: <br><br><b><a target='_blank' href='http://support.microsoft.com/?id=834010'>834010 A deadlock occurs when a program that uses WMI calls the LoadLibrary() or FreeLibrary() function in Windows 2000</b></a>";
						}
					}
					break;
				}
			}
		}
		return "";
	}

	private string GetRecommendationForLoaderLockIssue(CacheFunctions.ScriptThreadClass OwningThread, ref bool bGenericLoaderLockRecommendationGiven)
	{
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		string text = null;
		int num = 0;
		string text2 = "";
		string text3 = "";
		object obj = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		int num2 = 0;
		bool flag = false;
		int num3 = 0;
		string text4 = "";
		object obj2 = null;
		bGenericLoaderLockRecommendationGiven = false;
		scriptModuleClass = null;
		dictionary = OwningThread.StackFrames;
		for (num = 0; num <= Convert.ToInt32(dictionary.Count) - 1; num++)
		{
			scriptStackFrameClass = dictionary[num];
			text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
			switch (Convert.ToString(text).ToUpper())
			{
			case "NTDLL!LDRPCALLINITROUTINE":
			case "NTDLL!LDRPRUNINITIALIZEROUTINES":
			case "NTDLL!LDRPLOADDLL":
			case "NTDLL!LDRLOADDLL":
			case "NTDLL!LDRUNLOADDLL":
			case "KERNEL32!LOADLIBRARYEXA":
			case "KERNEL32!LOADLIBRARYEXW":
			case "KERNEL32!LOADLIBRARYA":
			case "KERNEL32!FREELIBRARY":
				for (num3 = num - 1; num3 >= 0; num3 += -1)
				{
					scriptStackFrameClass = dictionary[num3];
					scriptModuleClass = CacheFunctions.GetModuleFromAddress(scriptStackFrameClass.InstructionAddress);
					string text5 = Convert.ToString(scriptModuleClass.ToString()).ToUpper();
					if (!(text5 == "NTDLL") && !(text5 == "KERNEL32"))
					{
						num2 = num3;
					}
				}
				break;
			}
		}
		if (scriptModuleClass != null)
		{
			if (Globals.g_OSVER == Globals.OS_VER_WIN2K3)
			{
				string text5 = Convert.ToString(scriptModuleClass.ModuleName).ToLower();
				if (text5 == "mqsec" || text5 == "mqrt")
				{
					for (num3 = num2 - 1; num3 >= 0; num3 += -1)
					{
						scriptStackFrameClass = dictionary[num3];
						text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
						if (Convert.ToString(text).GetSafeLength() <= 5)
						{
							break;
						}
						text5 = Convert.ToString(text).ToLower().Substring(0, 5);
						if (!(text5 == "mqsec") && !(text5 == "mqrt!") && Convert.ToString(text).ToUpper() == "NTDLL!ETWREGISTERTRACEGUIDSW")
						{
							return "This may be caused by a known issue in Microsoft Message Queue Server 3.0. Please see the following Knowledge Base article for more information on how to obtain the hotfix: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=831844'>831844 - Message Queuing 3.0 may cause a process to stop responding</a></b>";
						}
					}
				}
			}
			if (Globals.g_OSVER == Globals.OS_VER_WIN2K3 && Convert.ToString(scriptModuleClass.ModuleName).ToLower() == "msctf" && Convert.ToInt64(OwningThread.WaitingOnCritSecAddr) != 0L && CacheFunctions.GetSymbolFromAddress(OwningThread.WaitingOnCritSecAddr).ToUpper() == Globals.g_COMRuntimeModule + "!GLOBALMUTEX")
			{
				for (num3 = num2 - 1; num3 >= 0; num3 += -1)
				{
					scriptStackFrameClass = dictionary[num3];
					text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
					switch (Convert.ToString(text).ToUpper())
					{
					case "MSCTF!CREATEPROPERSECURITYDESCRIPTOR":
					case "ADVAPI32!CONVERTSTRINGSECURITYDESCRIPTORTOSECURITYDESCRIPTORA":
					case "ADVAPI32!CONVERTSTRINGSECURITYDESCRIPTORTOSECURITYDESCRIPTOR":
						return "This may be caused by a known issue with the Language Bar on Micorosft Windows 2003, which is resolved in Service Pack 1 for Windows 2003. Please see the following Knowledge Base article for more information: <br><br> <b><a target='_blank' href='http://support.microsoft.com/?id=840620'>840620 - You cannot start a program when the Language bar is running on a Windows Server 2003-based multiprocessor computer</a></b>";
					}
				}
			}
			if (Convert.ToString(scriptModuleClass.ModuleName).ToUpper() == Globals.g_COMRuntimeModule)
			{
				for (num3 = num2 - 1; num3 >= num2 - 5 && num3 >= 0; num3 += -1)
				{
					scriptStackFrameClass = dictionary[num3];
					text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
					if (Convert.ToString(text).ToUpper() == Globals.g_COMRuntimeModule + "!COUNINITIALIZE")
					{
						text4 = Convert.ToString(Globals.g_Debugger.ExecutableName);
						text4 = text4.Substring(text4.GetSafeLength() - text4.GetSafeLength() - text4.LastIndexOf("\\"));
						return "This issue may have been caused by the application making a call to CoInitialize[Ex] without a corresponding call to CoUninitialze.  When this happens, ole32.dll will call CoUninitialize as it is unloaded - during which time it holds the LoaderLock.  This COM apartment cleanup can result in cross-thread calls which typically results in this type of deadlock.<br><br>The steps given in the following Microsoft Knowledge Base article can be used to record call stacks for all calls to these functions, which you can use to locate the source of this 'CoInitialize leak.'<br><br><a target='_blank' href='http://support.microsoft.com/default.aspx?scid=kb;EN-US;911359'>911359 -A client application may intermittently receive an error message when a client application tries to create a COM+ component</a>";
					}
				}
			}
		}
		obj = Globals.AnalyzeThreads.getAnalysis(OwningThread);
		text3 = Convert.ToString(TrimLongDescription(obj));
		if (text3 == null || text3.Length != 0)
		{
			switch (text3)
			{
			case "not fully resolved and may or may not be a problem.  Further analysis of this thread may be required":
			case "waiting on a critical section":
			case "making a COM call":
				break;
			default:
				goto IL_0681;
			}
		}
		for (num3 = num2 - 1; num3 >= 0; num3 += -1)
		{
			scriptStackFrameClass = dictionary[num3];
			text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
			if (!(Convert.ToString(text) != ""))
			{
				continue;
			}
			switch (Globals.HelperFunctions.Split(Convert.ToString(text), "!")[0].ToUpper())
			{
			case "OLE32":
			case "COMBASE":
				if (Convert.ToString(text).ToUpper() == Globals.g_COMRuntimeModule + "!COINITIALIZE")
				{
					text3 = "calling the CoInitialize API";
				}
				else if (Convert.ToString(text).ToUpper() == Globals.g_COMRuntimeModule + "!COUNINITIALIZE")
				{
					text3 = "calling the CoUnInitialize API";
				}
				else if (text3 == "")
				{
					text3 = "using OLE/COM functions";
				}
				break;
			case "ADVAPI32":
				flag = true;
				break;
			case "RPCRT4":
				text3 = "using RPC functions";
				if (flag)
				{
					text3 += " (indirectly via advapi32 functions)";
				}
				break;
			case "OLEAUT32":
				if (Convert.ToString(text).ToUpper() == "OLEAUT32!SYSALLOCSTRING")
				{
					text3 = "calling the SysAllocString API";
					obj2 = Globals.g_Debugger.ReadUnicodeString(scriptStackFrameClass.Args(0));
					if (Convert.ToString(obj2) != "")
					{
						text3 = text3 + " </b>to allocate the string <font color=blue>\"" + Convert.ToString(obj2) + "\"</font><b>";
					}
				}
				else
				{
					text3 = "using OLE Automation functions";
				}
				break;
			}
		}
		if (flag && text3 == "")
		{
			text3 = "using advapi32 functions";
		}
		goto IL_0681;
		IL_0681:
		text2 = "The entry-point function for a dynamic link library (DLL) should perform only simple initialization or termination tasks";
		if (text3 == "")
		{
			bGenericLoaderLockRecommendationGiven = true;
		}
		else
		{
			text2 = text2 + ", however this thread (" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(OwningThread.ThreadID) + ") is <b>" + text3 + "</b>");
		}
		text2 += ".  Follow the guidance in the MSDN documentation for <a href=\"http://msdn.microsoft.com/library/default.aspx?url=/library/en-us/dllproc/base/dllmain.aspx\">DllMain</a> to avoid access violations and deadlocks while loading and unloading libraries.<br><br>";
		if (scriptModuleClass != null)
		{
			text2 += "Please follow up with the vendor <b>";
			if (Convert.ToString(scriptModuleClass.VSCompanyName) != "")
			{
				text2 += Convert.ToString(scriptModuleClass.VSCompanyName);
			}
			return text2 + "</b> for <b>" + Convert.ToString(scriptModuleClass.ImageName) + "</b><br>";
		}
		return text2 + "<font color='Gray'>Note: DebugDiag could not determine which module was being loaded or unloaded on this thread. Typically this is due to unresolved symbols.</font>";
	}

	private string TrimLongDescription(object AnalyzedOwnerThread)
	{
		throw new NotImplementedException();
	}

	public string GetPreviousExceptionsRecommendationStringForOwnedLock(string nThreadNum, string sLockModuleName)
	{
		string text = null;
		string[] array = null;
		string[] array2 = null;
		int num = 0;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		new Dictionary<string, string>();
		object obj = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = new CacheFunctions.ScriptThreadClass();
		bool flag = false;
		if (scriptThreadClass == null)
		{
			return flag.ToString();
		}
		text = ((!(Convert.ToString(Globals.g_UtilExt.OSPlatformVersion) == "X64")) ? Globals.g_Debugger.Execute("~" + Convert.ToString(nThreadNum) + "e s -d poi(@$teb+8) poi(@$teb+4) 1003f") : Globals.g_Debugger.Execute("~" + Convert.ToString(nThreadNum) + "e s -d poi(@$teb+0x10) poi(@$teb+8) 1003f"));
		if (Convert.ToString(text).GetSafeLength() > 0)
		{
			array = Globals.HelperFunctions.Split(Convert.ToString(text), "\n");
			if (array.GetUpperBound(0) >= 1)
			{
				for (num = array.GetLowerBound(0); num <= array.GetUpperBound(0); num++)
				{
					array2 = Globals.HelperFunctions.Split(array[num], " ");
					if (array2.GetUpperBound(0) == 7)
					{
						dictionary.Add(array2[0], array2[0]);
					}
				}
			}
		}
		if (dictionary != null)
		{
			return flag.ToString();
		}
		if (dictionary.Count == 0)
		{
			return flag.ToString();
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (dictionary.Count == 1)
		{
			stringBuilder.Append("<br><br>Note evidence of a possible exception record was found on this thread, indicating an <b>earlier exception</b> ");
		}
		else
		{
			stringBuilder.Append("<br><br>Note evidence of possible exception records were found on this thread, indicating <b>earlier exceptions</b> ");
		}
		stringBuilder.Append("which may have leaked this lock.  See the thread report for thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(int.Parse(nThreadNum))) + " for more details.");
		if (Convert.ToBoolean(~Convert.ToInt32(Globals.g_collPreviousExceptions[nThreadNum])))
		{
			Globals.g_collPreviousExceptions.Add(nThreadNum, dictionary.Count.ToString());
			num = 0;
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				double fContextAdress = Globals.HelperFunctions.FromHex(item.Value);
				num++;
				if (scriptThreadClass.ChangeThreadContext(ref fContextAdress))
				{
					scriptThreadClass.FlushStackFrames();
					Globals.g_collPreviousExceptions.Add(Convert.ToString(nThreadNum) + ":" + Convert.ToString(num), scriptThreadClass.StackReportWithArgs);
				}
			}
			Globals.g_Debugger.Execute("~0s;~1s");
			Globals.g_Debugger.Execute("~" + Convert.ToString(nThreadNum) + "s");
			Globals.g_Debugger.Execute(".cxr");
			scriptThreadClass.FlushStackFrames();
		}
		obj = GetLockModuleNote(sLockModuleName, bOrphaned: false);
		if (Convert.ToString(obj) != "")
		{
			stringBuilder.Append(obj);
		}
		return stringBuilder.ToString();
	}

	public string GetPreviousExceptionsRecommendationStringForOrphanedLock(object sLockModuleName)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = new CacheFunctions.ScriptThreadClass();
		object obj = null;
		object obj2 = null;
		object obj3 = null;
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (num = 0; num <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; num++)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
			GetPreviousExceptionsRecommendationStringForOwnedLock(scriptThreadClass.ToString(), sLockModuleName.ToString());
		}
		if (Convert.ToInt32(Globals.g_collPreviousExceptions.Count) == 0)
		{
			return flag.ToString();
		}
		stringBuilder.Append("The thread which owned this lock is no longer present, but ");
		if (Convert.ToInt32(Globals.g_collPreviousExceptions.Count) == 2)
		{
			stringBuilder.Append("evidence of a possible exception record was found on another thread, indicating an <b>earlier exception</b>.  A similar exception on the missing thread may have crashed that thread and orphaned this lock.  See the <Font Color='Red'><b>Recovered Call Stack</b></Font> for thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink((int)obj2)) + " for more details.");
		}
		else
		{
			stringBuilder.Append("evidence of possible exception records were found on other threads, indicating <b>earlier exceptions.</b> A similar exception on the missing thread may have crashed that thread and orphaned this lock.  See the <Font Color='Red'><b>Recovered Call Stacks</b></Font> for the following threads for more details:<br><br>(Threads");
			foreach (KeyValuePair<string, string> g_collPreviousException in Globals.g_collPreviousExceptions)
			{
				if (g_collPreviousException.Key != string.Empty && Convert.ToString(obj).IndexOf(":", 0) == -1)
				{
					stringBuilder.Append(" " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(int.Parse(g_collPreviousException.Value))));
				}
			}
			stringBuilder.Append(" )");
		}
		obj3 = GetLockModuleNote(sLockModuleName, bOrphaned: true);
		if (Convert.ToString(obj3) != "")
		{
			stringBuilder.Append(obj3);
		}
		return stringBuilder.ToString();
	}

	private object GetLockModuleNote(object sLockModuleName, bool bOrphaned)
	{
		string text = "";
		object obj = null;
		StringBuilder stringBuilder = new StringBuilder();
		string text2 = Convert.ToString(sLockModuleName).ToUpper();
		if (text2 == null || text2.Length != 0)
		{
			switch (text2)
			{
			default:
				text = Convert.ToString(sLockModuleName).ToUpper().Replace(".DLL", "!");
				foreach (KeyValuePair<string, string> g_collPreviousException in Globals.g_collPreviousExceptions)
				{
					if (g_collPreviousException.Key != string.Empty && Convert.ToString(obj).IndexOf(":", 0) >= 0 && Globals.g_collPreviousExceptions[g_collPreviousException.Key].ToUpper().Contains(text))
					{
						stringBuilder.Append("<br><br>Note the lock belongs to <b>" + Convert.ToString(sLockModuleName) + "</b>, and this module appears on one or more of the recovered exception call stacks.  ");
						if (bOrphaned)
						{
							stringBuilder.Append("This increases the likelihood that a similar exception may have crashed the owning thread and orphaned this lock, especially if the module is not typically found on most call stacks in the process.");
						}
						else
						{
							stringBuilder.Append("This increases the likelihood that one of these exceptions could have leaked this lock.");
						}
						break;
					}
				}
				break;
			case "UNKNOWN_MODULE":
			case "NTDLL.DLL":
			case "OLE32.DLL":
			case "COMBASE.DLL":
			case "KERNEL32.DLL":
				break;
			}
		}
		return stringBuilder.ToString();
	}
}
