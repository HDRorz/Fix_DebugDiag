using System;
using System.Collections.Generic;
using ComplusDDExt;

namespace DebugDiag.AnalysisRules;

public class AnalyzeActivitiesImpl : IAnalyzeActivities
{
	public string GetActivityWithLink(IDbgActivity activity)
	{
		return "<a href='#" + Convert.ToString(Globals.g_UniqueReference) + "Activity" + Convert.ToString(activity.Address) + "'><b>" + Convert.ToString(Globals.HelperFunctions.GetAsHexString(activity.Address)) + "</b></a>";
	}

	public string GetActivityWithLinkRoot(IDbgActivity activity)
	{
		return "<b><a name='" + Convert.ToString(Globals.g_UniqueReference) + "Activity" + Convert.ToString(activity.Address) + "'>" + Convert.ToString(Globals.HelperFunctions.GetAsHexString(activity.Address)) + "</a></b>";
	}

	public string GetActivityData(IDbgActivity activity)
	{
		string text = "";
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		text = "Data for Activity at address " + GetActivityWithLinkRoot(activity);
		text = text + "<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + "State = " + Convert.ToString(activity.State);
		if (Convert.ToString(activity.State).ToUpper() != "UNENTERED")
		{
			text = text + "<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + "Entered By: Thread #" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(activity.OwnerThreadNum));
		}
		num = activity.WaitingThreadCount;
		text = text + "<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(5)) + Convert.ToString(num) + " thread" + Convert.ToString(Globals.HelperFunctions.IsAre(num)) + "blocked:";
		for (num2 = 1; num2 <= Convert.ToInt32(num); num2++)
		{
			num3 = activity.GetWaitingThreadByIndex(num2);
			text = text + "<br>" + Convert.ToString(Globals.HelperFunctions.Spaces(10)) + "Thread #" + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(num3));
		}
		return text;
	}

	public void ReportAllActivities()
	{
		ReportAllSimpleDeadlockedActivities();
		ReportAlBlockinglNonDeadlockedActivities();
		ReportAllTransitioningActivities();
	}

	public void ReportAllSimpleDeadlockedActivities()
	{
		string text = "";
		string text2 = "";
		int num = 0;
		Globals.HelperFunctions.ResetStatus("Reporting simple activity deadlocks", Globals.g_collDeadLockedActivites.Count, "Activity");
		foreach (KeyValuePair<CDbgActivity, double> g_collDeadLockedActivite in Globals.g_collDeadLockedActivites)
		{
			text = "The following activity in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> is deadlocked!<br><br>" + GetActivityData(g_collDeadLockedActivite.Key);
			text2 = ((!(Convert.ToString(g_collDeadLockedActivite.Key.State).ToUpper() == "DEADLOCKED")) ? (GetRecommendationForOrphanedActivity(g_collDeadLockedActivite.Key) + Convert.ToString(Globals.AnalyzeCritSecs.GetPreviousExceptionsRecommendationStringForOrphanedLock(""))) : (GetRecommendationForDeadlockedActivity(g_collDeadLockedActivite.Key) + Convert.ToString(Globals.AnalyzeCritSecs.GetPreviousExceptionsRecommendationStringForOwnedLock(g_collDeadLockedActivite.Key.OwnerThreadNum.ToString(), ""))));
			num = Convert.ToInt32(g_collDeadLockedActivite.Key.WaitingThreadCount) * 10;
			Globals.Manager.ReportError(text, text2, num, "{623b214e-aafc-4e42-b22e-a382cc08c4c7}");
			Globals.g_IsComPlusSTAPoolIssueDetected = true;
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public void ReportAlBlockinglNonDeadlockedActivities()
	{
		CDbgActivity cDbgActivity = null;
		string text = "";
		string text2 = "";
		int num = 0;
		if (Convert.ToInt32(Globals.g_collDeadLockedActivites.Count) != 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Reporting non-deadlocked activities", Globals.g_collEnteredButNonDeadlockedActivites.Count, "Activity");
		foreach (KeyValuePair<CDbgActivity, double> g_collEnteredButNonDeadlockedActivite in Globals.g_collEnteredButNonDeadlockedActivites)
		{
			cDbgActivity = g_collEnteredButNonDeadlockedActivite.Key;
			text = "The following activity in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> is blocking at least one thread.<br><br>" + GetActivityData(cDbgActivity);
			text2 = GetRecommendationForBlockingActivity(cDbgActivity) + Convert.ToString(Globals.AnalyzeCritSecs.GetPreviousExceptionsRecommendationStringForOwnedLock(cDbgActivity.OwnerThreadNum.ToString(), ""));
			num = Convert.ToInt32(cDbgActivity.WaitingThreadCount) * 10;
			Globals.Manager.ReportWarning(text, text2, num, "{10eb5d48-2060-42d9-978a-4b1a67eb2f31}");
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public void ReportAllTransitioningActivities()
	{
		string text = "";
		string text2 = "";
		int num = 0;
		if (Convert.ToInt32(Globals.g_collDeadLockedActivites.Count) != 0 || Convert.ToInt32(Globals.g_collEnteredButNonDeadlockedActivites.Count) != 0)
		{
			return;
		}
		Globals.HelperFunctions.ResetStatus("Reporting transitioning activities", Globals.g_collTransitioningActivites.Count, "Activity");
		foreach (KeyValuePair<CDbgActivity, double> g_collTransitioningActivite in Globals.g_collTransitioningActivites)
		{
			text = "The following activity in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b> is transitioning. ";
			num = Convert.ToInt32(g_collTransitioningActivite.Key.WaitingThreadCount) * 10;
			if (Convert.ToInt32(g_collTransitioningActivite.Key.WaitingThreadCount) > 1)
			{
				text = text + "This activity is not currently entered by any thread, but " + Convert.ToString(g_collTransitioningActivite.Key.WaitingThreadCount) + " threads are blocked on this activity, so it could represent a lock convoy with many threads juggling over the same activity lock.<br><br>" + GetActivityData(g_collTransitioningActivite.Key);
				text2 = GetRecommendationForTransitioningActivity(g_collTransitioningActivite.Key) + Convert.ToString(Globals.AnalyzeCritSecs.GetPreviousExceptionsRecommendationStringForOwnedLock(g_collTransitioningActivite.Key.OwnerThreadNum.ToString(), ""));
				Globals.Manager.ReportWarning(text, text2, num, "{8ddbb4bd-661e-4bd1-8c21-7dacdfeffef1}");
			}
			else
			{
				text = text + "Note only one thread is blocking on this activity, so this is most likely not causing a problem.<br><br>" + GetActivityData(g_collTransitioningActivite.Key);
				Globals.Manager.ReportInformation(text, 0, "{90ebeb48-e63a-408e-aa2c-ef2e566bcf01}");
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public void SortAllActivities()
	{
		Globals.HelperFunctions.ResetStatus("Sorting all blocking activities", Globals.g_ComplusExt.BlockingActivityInfo.Count, "Activity");
		foreach (CDbgActivity item in Globals.g_ComplusExt.BlockingActivityInfo)
		{
			switch (Convert.ToString(item.State).ToUpper())
			{
			case "DEADLOCKED":
			case "ORPHANED":
				Globals.g_collDeadLockedActivites.Add(item, item.Address);
				break;
			case "ENTERED":
				if (Convert.ToInt32(Globals.g_collDeadLockedActivites.Count) == 0)
				{
					Globals.g_collEnteredButNonDeadlockedActivites.Add(item, item.Address);
				}
				break;
			case "UNENTERED":
				if (Convert.ToInt32(Globals.g_collDeadLockedActivites.Count) == 0 && Convert.ToInt32(Globals.g_collEnteredButNonDeadlockedActivites.Count) == 0)
				{
					Globals.g_collTransitioningActivites.Add(item, item.Address);
				}
				break;
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public string GetRecommendationForTransitioningActivity(IDbgActivity activity)
	{
		return "Threads blocking on a COM+ activity can cause a performance bottleneck due to serialization.  Analyze the call stacks for the blocking threads provided in the Description pane to determine why multiple threads are trying to enter the same activity.";
	}

	public string GetRecommendationForBlockingActivity(IDbgActivity activity)
	{
		return "Threads blocking on a COM+ activity can cause a performance bottleneck due to serialization." + " Note this script currently detects only \"pure\" COM+ activity deadlocks, and does not detect any \"mixed\" deadlocks (deadlocks including other lock types, cross-thread COM calls, etc.), so further analysis of the threads in the Description pane is recommended, to determine if a \"mixed\" deadlock exists.";
	}

	public string GetRecommendationForOrphanedActivity(CDbgActivity activity)
	{
		string text = "";
		string text2 = "The activity at " + GetActivityWithLink(activity) + " has been orphaned, which means the owning thread is no longer present in the process.  In most cases this is due to an <b>earlier exception</b> causing  the thread to terminate without leaving (unlocking) the activity.  Please use a DebugDiag crash rule to monitor the application for first chance exceptions, particularly access violations. If an exception is found on a thread while owning the activity, then address the root cause for that exception.";
		text = Convert.ToString(Globals.AnalyzeCritSecs.GetPreviousExceptionsRecommendationStringForOwnedLock("999", ""));
		if (text.GetSafeLength() > 0)
		{
			text = "<br><br>" + text;
		}
		return text2 + text;
	}

	public string GetRecommendationForDeadlockedActivity(IDbgActivity activity)
	{
		string text = "";
		bool flag = false;
		int num = 0;
		int currentPosition = 0;
		Globals.HelperFunctions.ResetStatus("Scanning thread for known activity deadlock causes", activity.WaitingThreadCount, "Thread");
		for (num = 1; num <= Convert.ToInt32(activity.WaitingThreadCount); num++)
		{
			activity.GetWaitingThreadByIndex(num);
			if (Globals.g_ThreadInfoCache.Item(num).FindFrameInStack(Globals.g_COMRuntimeModule + "!CGIPTABLE") > -1)
			{
				flag = true;
			}
			if (flag)
			{
				Globals.g_Progress.CurrentPosition = currentPosition;
				break;
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		if (flag)
		{
			text = "This COM+ activity deadlock is due to a known issue. Please follow the steps outlined in the following Knowledge Base articles: <br> <a target='_blank' href='http://support.microsoft.com/?id=298014'> 298014 FIX: COM+ application that uses the Global Interface Table (GIT) may deadlock </a><br> <a target='_blank' href='http://support.microsoft.com/?id=319579'> 319579 COM Activity Deadlock Causes IIS to Stop Responding </a>";
			if (Convert.ToBoolean(Globals.HelperFunctions.IsModuleLoaded("SYSTEM_ENTERPRISESERVICES")))
			{
				text += "<br><br>Note various registry keys are suggested as workarounds but the overarching requirement is to <b>call the .Dispose method on all .NET ServicedComponents</b>.  See the <a href=http://www.gotdotnet.com/team/xmlentsvcs/esfaq.aspx#1.1>Enterprise Services FAQ</a> for more information.";
			}
		}
		else
		{
			text = "This COM+ activity deadlock is not due to a known issue.  One possibility is that a previous exception occured on Thread " + Convert.ToString(Globals.HelperFunctions.GetThreadIDWithLink(activity.OwnerThreadNum)) + ", causing the thread to terminate without leaving (unlocking) the activity.  Please use a DebugDiag crash rule to monitor the application for first chance exceptions, particularly access violations. If an exception is found on a thread while owning a COM+ activity, then address the root cause for that exception.";
		}
		return text;
	}
}
