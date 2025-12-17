using System;
using System.Collections.Generic;
using System.Linq;
using DebugDiag.DotNet.Reports;
using IISInfoLib;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

internal class LeakFunctions
{
	private static Dictionary<double, ILTModule> GetModuleDictionary(object obj)
	{
		object[,] array = (object[,])obj;
		Dictionary<double, ILTModule> dictionary = new Dictionary<double, ILTModule>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			dictionary.Add((double)array[i, 0], (ILTModule)array[i, 1]);
		}
		return dictionary;
	}

	private static Dictionary<double, ILTType> GetLTTypeDictionary(object obj)
	{
		object[,] array = (object[,])obj;
		Dictionary<double, ILTType> dictionary = new Dictionary<double, ILTType>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			dictionary.Add((double)array[i, 0], (ILTType)array[i, 1]);
		}
		return dictionary;
	}

	private static Dictionary<double, ILTFunction> GetLTFunctionDictionary(object obj)
	{
		object[,] array = (object[,])obj;
		Dictionary<double, ILTFunction> dictionary = new Dictionary<double, ILTFunction>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			dictionary.Add((double)array[i, 0], (ILTFunction)array[i, 1]);
		}
		return dictionary;
	}

	private static Dictionary<double, ISizeInfo> GetSizeInfoDictionary(object obj)
	{
		object[,] array = (object[,])obj;
		Dictionary<double, ISizeInfo> dictionary = new Dictionary<double, ISizeInfo>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			dictionary.Add((double)array[i, 0], (ISizeInfo)array[i, 1]);
		}
		return dictionary;
	}

	private static Dictionary<double, ILTHeapInfo> GetLTHeapInfoDictionary(object obj)
	{
		object[,] array = (object[,])obj;
		Dictionary<double, ILTHeapInfo> dictionary = new Dictionary<double, ILTHeapInfo>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			dictionary.Add((double)array[i, 0], (ILTHeapInfo)array[i, 1]);
		}
		return dictionary;
	}

	private static string[,] GetSynStack(object obj)
	{
		object[,] array = (object[,])obj;
		if (array.GetLength(1) != 3)
		{
			throw new IndexOutOfRangeException("Object Array returned from LTFunction::get_CallStack has an invalid Length.  It should Always be 3");
		}
		string[,] array2 = new string[array.GetLength(0), array.GetLength(1)];
		for (int i = 0; i < array.GetLength(0); i++)
		{
			for (int j = 0; j < array.GetLength(1); j++)
			{
				array2[i, j] = (string)array[i, j];
			}
		}
		return array2;
	}

	public static void AnalyzeAndReportLTData()
	{
		Globals.g_Weight = 1000;
		ReportSection val = Globals.Manager.AddReportSection("LeakReport", (SectionType)0);
		val.Title = "Leak analysis";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("<b>LeakTrack Version Loaded: ");
		Globals.Manager.Write(GetLTVersion());
		Globals.Manager.Write("</b>\r\n");
		ReportSection val2 = val.AddChildSection("AllocSummary", (SectionType)0);
		val2.Title = "Outstanding allocation summary";
		Globals.Manager.CurrentSection = val2;
		Globals.Manager.Write("<table class=myCustomText ID=\"Table1\">\r\n");
		Globals.Manager.Write("<tr>\r\n");
		Globals.Manager.Write("<td>Number of allocations</td>\r\n");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(FormatNumber(Globals.g_LeakTrackInfo.AllocationCount, 0));
		Globals.Manager.Write(" allocations</b></td>\r\n");
		Globals.Manager.Write("</tr>\r\n");
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.Manager.Write("<tr>\r\n");
		Globals.Manager.Write("<td>Total outstanding handle count</td>\r\n");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(FormatNumber(Globals.g_LeakTrackInfo.HandleCount, 0));
		Globals.Manager.Write(" handles</b></td>\r\n");
		Globals.Manager.Write("</tr>\r\n");
		Globals.Manager.Write("<tr>\r\n");
		Globals.Manager.Write("<td>Total size of allocations</td>\r\n");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(Globals.g_LeakTrackInfo.AllocationSize));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("</tr>\r\n");
		Globals.Manager.Write("<tr>\r\n");
		Globals.Manager.Write("<td>Tracking duration</td>\r\n");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintTime(Globals.g_LeakTrackInfo.Duration));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("</tr>\r\n");
		Globals.Manager.Write("</table>\r\n");
		if (string.Compare(FormatNumber(Globals.g_LeakTrackInfo.AllocationCount, 0), "0") > 0)
		{
			Globals.g_ModuleArray = LeakFunctions.GetModuleDictionary((dynamic)Globals.g_LeakTrackInfo.ModulesByCount);
			PrintModuleGraph(Globals.g_ModuleArray, 1, bHandles: false);
			Globals.g_ModuleArray = LeakFunctions.GetModuleDictionary((dynamic)Globals.g_LeakTrackInfo.ModulesBySize);
			PrintModuleGraph(Globals.g_ModuleArray, 0, bHandles: false);
			Globals.g_LTTypeArray = LeakFunctions.GetLTTypeDictionary((dynamic)Globals.g_LeakTrackInfo.LTTypesByCount);
			PrintLTTypeGraph(Globals.g_LTTypeArray, 1, bHandles: false);
			Globals.g_LTTypeArray = LeakFunctions.GetLTTypeDictionary((dynamic)Globals.g_LeakTrackInfo.LTTypesBySize);
			PrintLTTypeGraph(Globals.g_LTTypeArray, 0, bHandles: false);
			Globals.g_ModuleArray = LeakFunctions.GetModuleDictionary((dynamic)Globals.g_LeakTrackInfo.HandleModulesByCount);
			PrintModuleGraph(Globals.g_ModuleArray, 1, bHandles: true);
			Globals.g_LTTypeArray = LeakFunctions.GetLTTypeDictionary((dynamic)Globals.g_LeakTrackInfo.HandleTypesByCount);
			PrintLTTypeGraph(Globals.g_LTTypeArray, 1, bHandles: true);
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.g_ModuleArray = LeakFunctions.GetModuleDictionary((dynamic)Globals.g_LeakTrackInfo.ModulesBySize);
			Globals.HelperFunctions.ResetStatus("Generating detailed module report (Memory)", Globals.g_ModuleArray.Keys.Count, "Module");
			val2 = val.AddChildSection("ModReportMem", (SectionType)0);
			val2.Title = "Detailed module report(Memory)";
			Globals.Manager.CurrentSection = val2;
			foreach (KeyValuePair<double, ILTModule> item in Globals.g_ModuleArray)
			{
				PrintLTModuleDetails(item.Key, item.Value, -1, bHandles: false);
				Globals.HelperFunctions.IncrementSubStatus();
			}
			Globals.HelperFunctions.UpdateOverallProgress();
			Globals.g_ModuleArray = LeakFunctions.GetModuleDictionary((dynamic)Globals.g_LeakTrackInfo.HandleModulesByCount);
			Globals.HelperFunctions.ResetStatus("Generating detailed module report (Handles)", Globals.g_ModuleArray.Keys.Count, "Module");
			val2 = val.AddChildSection("ModReportHandle", (SectionType)0);
			val2.Title = "Detailed module report(Handles)";
			Globals.Manager.CurrentSection = val2;
			foreach (KeyValuePair<double, ILTModule> item2 in Globals.g_ModuleArray)
			{
				PrintLTModuleDetails(item2.Key, item2.Value, -1, bHandles: true);
				Globals.HelperFunctions.IncrementSubStatus();
			}
			if (Globals.g_bFunctionTracked && !Globals.g_bStackCollected)
			{
				Globals.Manager.ReportOther("LeakTrack was detected in " + Convert.ToString(Globals.g_ShortDumpFileName) + " and allocations were tracked, however, no call stacks were recorded because the tracking duration was to short.  Either track the leak for longer than <b>15 minutes</b>, or select <b>Options and Settings</b> from the <b>Tools</b> menu and enable the <b>Begin recording call stacks immediately when monitoring for leaks</b> option in the <b>Preferences</b> tab.", "", "Notification", "notificationicon.png", 0, "{114e8255-e934-4e64-994b-43126af40f16}");
			}
		}
		else if (IsLowerLTVersion())
		{
			Globals.Manager.ReportOther("LeakTrack was detected in " + Convert.ToString(Globals.g_ShortDumpFileName) + "Please use Debug Diagnostic Tool version 1.1 to analyze the dump. Note that version 1.1 and 1.2 cannot be installed on the same machine at the same time", "", "Notification", "notificationicon.png", 0, "{5fbef48e-dc62-41b3-af7c-f8daf7277a6b}");
		}
		else
		{
			Globals.Manager.ReportOther("LeakTrack was detected in " + Convert.ToString(Globals.g_ShortDumpFileName) + ", however, no allocations were tracked. Please make sure you have the latest version of LeakTrack.dll properly injected into the target process.", "", "Notification", "notificationicon.png", 0, "{7926aee6-cdbf-4687-844d-de80fea06df9}");
		}
		Globals.Manager.CurrentSection = val.Parent;
		Globals.HelperFunctions.UpdateOverallProgress();
	}

	private static string GetLTName(int LTTypeID)
	{
		string result = "";
		switch (LTTypeID)
		{
		case 0:
			result = "Heap memory Globals.Manager";
			break;
		case 1:
			result = "C/C++ runtime memory Globals.Manager";
			break;
		case 2:
			result = "OLE/COM memory Globals.Manager";
			break;
		case 3:
			result = "OLE automation BSTR memory Globals.Manager";
			break;
		case 4:
			result = "Virtual memory Globals.Manager";
			break;
		case 5:
			result = "Socket handles";
			break;
		case 6:
			result = "Mutex handles";
			break;
		case 7:
			result = "Semaphore handles";
			break;
		case 8:
			result = "Event handles";
			break;
		case 9:
			result = "Timer Queue handles";
			break;
		case 10:
			result = "Timer Queue Timer handles";
			break;
		case 11:
			result = "Waitable Timer handles";
			break;
		case 12:
			result = "File handles";
			break;
		case 13:
			result = "File Mapping handles";
			break;
		case 14:
			result = "I/O Completion port handles";
			break;
		case 15:
			result = "Anonymous pipe handles";
			break;
		case 16:
			result = "Named pipe handles";
			break;
		case 17:
			result = "Security Token handles";
			break;
		case 18:
			result = "File Change Notify handles";
			break;
		case 19:
			result = "Console Screen Buffer handles";
			break;
		case 20:
			result = "Desktop handles";
			break;
		case 21:
			result = "Window Station handles";
			break;
		case 22:
			result = "Event Log Source handles";
			break;
		case 23:
			result = "Event Log handles";
			break;
		case 24:
			result = "Job Object handles";
			break;
		case 25:
			result = "Mailslot handles";
			break;
		case 26:
			result = "Thread handles";
			break;
		case 27:
			result = "Process handles";
			break;
		case 28:
			result = "Registry Key handles";
			break;
		}
		return result;
	}

	private static string GetAllocType(int AllocTypeID)
	{
		string result = "";
		switch (AllocTypeID)
		{
		case 0:
			result = "Heap allocation(s)";
			break;
		case 1:
			result = "C/C++ runtime allocation(s)";
			break;
		case 2:
			result = "OLE/COM allocation(s)";
			break;
		case 3:
			result = "BSTR allocation(s)";
			break;
		case 4:
			result = "Virtual memory allocation(s)";
			break;
		case 5:
			result = "Socket handle(s)";
			break;
		case 6:
			result = "Mutex handle(s)";
			break;
		case 7:
			result = "Semaphore handle(s)";
			break;
		case 8:
			result = "Event handle(s)";
			break;
		case 9:
			result = "Timer Queue handle(s)";
			break;
		case 10:
			result = "Timer Queue Timer handle(s)";
			break;
		case 11:
			result = "Waitable Timer handle(s)";
			break;
		case 12:
			result = "File handle(s)";
			break;
		case 13:
			result = "File Mapping handle(s)";
			break;
		case 14:
			result = "I/O Completion port handle(s)";
			break;
		case 15:
			result = "Anonymous pipe handle(s)";
			break;
		case 16:
			result = "Named pipe handle(s)";
			break;
		case 17:
			result = "Security Token handle(s)";
			break;
		case 18:
			result = "File Change Notify handle(s)";
			break;
		case 19:
			result = "Console Screen Buffer handle(s)";
			break;
		case 20:
			result = "Desktop handle(s)";
			break;
		case 21:
			result = "Window Station handle(s)";
			break;
		case 22:
			result = "Event Log Source handle(s)";
			break;
		case 23:
			result = "Event Log handle(s)";
			break;
		case 24:
			result = "Job Object handle(s)";
			break;
		case 25:
			result = "Mailslot handle(s)";
			break;
		case 26:
			result = "Thread handle(s)";
			break;
		case 27:
			result = "Process handle(s)";
			break;
		case 28:
			result = "Registry Key handle(s)";
			break;
		}
		return result;
	}

	private static void PrintLTTypeGraph(Dictionary<double, ILTType> LTTypeArray, int SORT_TYPE, bool bHandles)
	{
		BarGraph barGraph = new BarGraph();
		ILTType iLTType = null;
		int num = 0;
		double num2 = 0.0;
		string caption = "";
		int num3 = 0;
		barGraph.SetRowCount(LTTypeArray.Keys.Count);
		switch (SORT_TYPE)
		{
		case 0:
			Globals.Manager.Write("<br><br><h4>Memory Globals.Manager statistics by allocation size</h4>");
			break;
		case 1:
			if (bHandles)
			{
				Globals.Manager.Write("<br><br><h4>Handle type statistics by handle count</h4>");
			}
			else
			{
				Globals.Manager.Write("<br><br><h4>Memory Globals.Manager statistics by allocation count</h4>");
			}
			break;
		case 2:
			if (bHandles)
			{
				Globals.Manager.Write("<br><br><h4>Handle type statistics by leak probability</h4>");
			}
			else
			{
				Globals.Manager.Write("<br><br><h4>Memory Globals.Manager statistics by leak probability</h4>");
			}
			break;
		}
		for (num3 = 0; num3 < LTTypeArray.Keys.Count; num3++)
		{
			num = Convert.ToInt32(LTTypeArray.Keys.ElementAt(num3));
			iLTType = LTTypeArray.Values.ElementAt(num3);
			switch (SORT_TYPE)
			{
			case 0:
				num2 = iLTType.AllocationSize;
				caption = Globals.HelperFunctions.PrintMemory(num2);
				break;
			case 1:
				num2 = iLTType.AllocationCount;
				caption = ((!bHandles) ? (FormatNumber(num2, 0) + " allocation(s)") : (FormatNumber(num2, 0) + " handle(s)"));
				break;
			case 2:
				num2 = 100.0 - Math.Abs(Convert.ToDouble(iLTType.LeakProbability));
				caption = ((!(num2 < Convert.ToDouble(Globals.TopLeakProbability))) ? ("<b>" + Convert.ToString(num2) + "%</b>") : (Convert.ToString(num2) + "%"));
				break;
			}
			barGraph.Rows[num3].Value = num2;
			barGraph.Rows[num3].Caption = "<b>" + GetLTName(num) + "</b>";
			barGraph.Rows[num3].Caption2 = caption;
		}
		barGraph.DrawGraph();
	}

	private static void PrintSizeInfoGraph(Dictionary<double, ISizeInfo> SizeInfoArray, int SORT_TYPE)
	{
		BarGraph barGraph = null;
		dynamic val = null;
		double num = 0.0;
		double num2 = 0.0;
		string text = "";
		string caption = "";
		int num3 = 0;
		num3 = ((SizeInfoArray.Keys.Count > Convert.ToInt32(Globals.TopSizeLimit)) ? Convert.ToInt32(Globals.TopSizeLimit) : SizeInfoArray.Keys.Count);
		switch (SORT_TYPE)
		{
		case 0:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " allocation sizes by total size</h6>");
			break;
		case 1:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " allocation sizes by allocation count</h6>");
			break;
		case 2:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " allocation sizes by leak probability</h6>");
			break;
		}
		barGraph = new BarGraph();
		barGraph.SetRowCount(num3);
		for (int i = 0; i <= num3 - 1; i++)
		{
			num = SizeInfoArray.Keys.ElementAt(i);
			val = SizeInfoArray.Values.ElementAt(i);
			text = Globals.HelperFunctions.PrintMemory(num);
			switch (SORT_TYPE)
			{
			case 0:
				num2 = Convert.ToDouble(val.AllocationSize);
				caption = Globals.HelperFunctions.PrintMemory(num2);
				break;
			case 1:
				num2 = Convert.ToDouble(val.AllocationCount);
				caption = FormatNumber(num2, 0) + " allocation(s)";
				break;
			case 2:
				num2 = 100.0 - Math.Abs(Convert.ToDouble(val.LeakProbability));
				caption = ((!(num2 < Convert.ToDouble(Globals.TopLeakProbability))) ? ("<b>" + Convert.ToString(num2) + "%</b>") : (Convert.ToString(num2) + "%"));
				break;
			}
			barGraph.Rows[i].Value = num2;
			barGraph.Rows[i].Caption = text;
			barGraph.Rows[i].Caption2 = caption;
		}
		barGraph.DrawGraph();
	}

	private static void PrintLTHeapGraph(Dictionary<double, ILTHeapInfo> LTHeapInfoArray, int SORT_TYPE)
	{
		BarGraph barGraph = null;
		dynamic val = null;
		double num = 0.0;
		double num2 = 0.0;
		string text = "";
		string caption = "";
		int num3 = 0;
		num3 = ((LTHeapInfoArray.Keys.Count > Convert.ToInt32(Globals.TopHeapLimit)) ? Convert.ToInt32(Globals.TopHeapLimit) : LTHeapInfoArray.Keys.Count);
		switch (SORT_TYPE)
		{
		case 0:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " heaps by total size</h6>");
			break;
		case 1:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " heaps by allocation count</h6>");
			break;
		case 2:
			Globals.Manager.Write("<h6>Top " + Convert.ToString(num3) + " heaps by leak probability</h6>");
			break;
		}
		barGraph = new BarGraph();
		barGraph.SetRowCount(num3);
		for (int i = 0; i <= num3 - 1; i++)
		{
			num = LTHeapInfoArray.Keys.ElementAt(i);
			val = LTHeapInfoArray.Values.ElementAt(i);
			text = Globals.HelperFunctions.GetAsHexString(num);
			switch (SORT_TYPE)
			{
			case 0:
				num2 = Convert.ToDouble(val.AllocationSize);
				caption = Globals.HelperFunctions.PrintMemory(num2);
				break;
			case 1:
				num2 = Convert.ToDouble(val.AllocationCount);
				caption = FormatNumber(num2, 0) + " allocation(s)";
				break;
			case 2:
				num2 = 100.0 - Math.Abs(Convert.ToDouble(val.LeakProbability));
				caption = ((!(num2 < 90.0)) ? ("<b>" + Convert.ToString(num2) + "%</b>") : (Convert.ToString(num2) + "%"));
				break;
			}
			barGraph.Rows[i].Value = num2;
			barGraph.Rows[i].Caption = text;
			barGraph.Rows[i].Caption2 = caption;
		}
		barGraph.DrawGraph();
	}

	private static string GetModuleLink(double ModuleBase)
	{
		return "#" + Convert.ToString(Globals.g_ShortDumpFileName) + "Module" + Convert.ToString(ModuleBase);
	}

	private static string GetHandleModuleLink(double ModuleBase)
	{
		return "#" + Convert.ToString(Globals.g_ShortDumpFileName) + "HandleModule" + Convert.ToString(ModuleBase);
	}

	private static string GetFunctionLink(double ReturnAddress)
	{
		return "#" + Convert.ToString(Globals.g_ShortDumpFileName) + "Function" + Convert.ToString(ReturnAddress);
	}

	private static string GetHandleFunctionLink(double ReturnAddress)
	{
		return "#" + Convert.ToString(Globals.g_ShortDumpFileName) + "HandleFunction" + Convert.ToString(ReturnAddress);
	}

	private static void PrintModuleGraph(Dictionary<double, ILTModule> ModuleArray, int SORT_TYPE, bool bHandles)
	{
		BarGraph barGraph = null;
		double num = 0.0;
		ILTModule iLTModule = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		double num2 = 0.0;
		string text = "";
		string caption = "";
		int num3 = 0;
		int num4 = 0;
		string text2 = "";
		string[] array = null;
		if (ModuleArray.Keys.Count < Globals.TopModuleLimit)
		{
			num3 = ModuleArray.Keys.Count;
		}
		else
		{
			num3 = Convert.ToInt32(Globals.TopModuleLimit);
		}
		num3 = ModuleArray.Keys.Count;
		barGraph = new BarGraph();
		barGraph.SetRowCount(num3);
		switch (SORT_TYPE)
		{
		case 0:
			Globals.Manager.Write("<br><br><h4>Top " + Convert.ToString(num3) + " modules by allocation size</h4>");
			break;
		case 1:
			if (Convert.ToBoolean(bHandles))
			{
				Globals.Manager.Write("<br><br><h4>Top " + Convert.ToString(num3) + " modules by handle count</h4>");
			}
			else
			{
				Globals.Manager.Write("<br><br><h4>Top " + Convert.ToString(num3) + " modules by allocation count</h4>");
			}
			break;
		case 2:
			Globals.Manager.Write("<br><br><h4>Top " + Convert.ToString(num3) + " modules by leak probability</h4>");
			break;
		}
		for (num4 = 0; num4 < num3; num4++)
		{
			num = ModuleArray.Keys.ElementAt(num4);
			scriptModuleClass = CacheFunctions.GetModuleFromAddress(num);
			iLTModule = ModuleArray.Values.ElementAt(num4);
			Math.Abs(Convert.ToDouble(iLTModule.LeakProbability));
			text2 = Globals.HelperFunctions.CheckSymbolType(num);
			array = Globals.HelperFunctions.Split(Convert.ToString(text2), ":", -1);
			text = ((scriptModuleClass != null) ? ("<b>" + Convert.ToString(scriptModuleClass.ModuleName) + "</b>") : ((!(array[1] == "1")) ? ("Unknown module - Base address : " + Globals.HelperFunctions.GetAsHexString(num)) : ("<b>" + array[0] + "</b>")));
			switch (SORT_TYPE)
			{
			case 0:
				num2 = Convert.ToDouble(iLTModule.AllocationSize);
				caption = Globals.HelperFunctions.PrintMemory(num2);
				if (num4 < 2)
				{
					ScanForKnownLeaks(iLTModule, num, text2);
				}
				break;
			case 1:
				num2 = Convert.ToDouble(iLTModule.AllocationCount);
				caption = ((!bHandles) ? (FormatNumber(num2, 0) + " allocation(s)") : (FormatNumber(num2, 0) + " handle(s)"));
				break;
			case 2:
				num2 = 100.0 - Math.Abs(Convert.ToDouble(iLTModule.LeakProbability));
				caption = ((!(num2 < Convert.ToDouble(Globals.TopLeakProbability))) ? ("<b>" + Convert.ToString(num2) + "%</b>") : (Convert.ToString(num2) + "%"));
				break;
			}
			barGraph.Rows[num4].Value = num2;
			barGraph.Rows[num4].Caption = text;
			barGraph.Rows[num4].Caption2 = caption;
			if (bHandles)
			{
				barGraph.Rows[num4].Link = GetHandleModuleLink(num);
			}
			else
			{
				barGraph.Rows[num4].Link = GetModuleLink(num);
			}
		}
		barGraph.DrawGraph();
	}

	private static void PrintFunctionGraph(Dictionary<double, ILTFunction> FunctionArray, int SORT_TYPE, bool bHandles)
	{
		BarGraph barGraph = null;
		double num = 0.0;
		ILTFunction iLTFunction = null;
		double num2 = 0.0;
		string text = "";
		string caption = "";
		int num3 = 0;
		int num4 = 0;
		num3 = ((FunctionArray.Keys.Count >= Convert.ToInt32(Globals.TopNumFunctionsLimit)) ? Convert.ToInt32(Globals.TopNumFunctionsLimit) : FunctionArray.Keys.Count);
		barGraph = new BarGraph();
		barGraph.SetRowCount(num3);
		switch (SORT_TYPE)
		{
		case 0:
			Globals.Manager.Write("<h5>Top " + Convert.ToString(num3) + " functions by allocation size</h5>");
			break;
		case 1:
			if (Convert.ToBoolean(bHandles))
			{
				Globals.Manager.Write("<h5>Top " + Convert.ToString(num3) + " functions by handle count</h5>");
			}
			else
			{
				Globals.Manager.Write("<h5>Top " + Convert.ToString(num3) + " functions by allocation count</h5>");
			}
			break;
		case 2:
			Globals.Manager.Write("<h5>Top " + Convert.ToString(num3) + " functions by leak probability</h5>");
			break;
		}
		for (num4 = 0; num4 <= num3 - 1; num4++)
		{
			num = FunctionArray.Keys.ElementAt(num4);
			iLTFunction = FunctionArray.Values.ElementAt(num4);
			text = "<b>" + CacheFunctions.GetSymbolFromAddress(num) + "</b>";
			switch (SORT_TYPE)
			{
			case 0:
				num2 = Convert.ToDouble(iLTFunction.AllocationSize);
				caption = Globals.HelperFunctions.PrintMemory(num2);
				break;
			case 1:
				num2 = Convert.ToDouble(iLTFunction.AllocationCount);
				caption = ((!Convert.ToBoolean(bHandles)) ? (FormatNumber(num2, 0) + " allocation(s)") : (FormatNumber(num2, 0) + " handle(s)"));
				break;
			case 2:
				num2 = 100.0 - Math.Abs(Convert.ToDouble(iLTFunction.LeakProbability));
				caption = ((!(num2 < Convert.ToDouble(Globals.TopLeakProbability))) ? ("<b>" + Convert.ToString(num2) + "%</b>") : (Convert.ToString(num2) + "%"));
				break;
			}
			barGraph.Rows[num4].Value = num2;
			barGraph.Rows[num4].Caption = text;
			barGraph.Rows[num4].Caption2 = caption;
			if (Convert.ToBoolean(bHandles))
			{
				barGraph.Rows[num4].Link = GetHandleFunctionLink(num);
			}
			else
			{
				barGraph.Rows[num4].Link = GetFunctionLink(num);
			}
		}
		barGraph.DrawGraph();
	}

	private static void PrintLTFunctionDetails(double ReturnAddress, ILTFunction LTFunction, bool bHandles)
	{
		int num = 0;
		string[,] array = null;
		int num2 = 0;
		int num3 = 0;
		string text = null;
		string text2 = "";
		IMemoryAllocation memoryAllocation = null;
		ILTHandleAllocation iLTHandleAllocation = null;
		Dictionary<double, ISizeInfo> sizeInfoArray = LeakFunctions.GetSizeInfoDictionary((dynamic)LTFunction.SizeInfosByCount);
		Dictionary<double, ISizeInfo> sizeInfoArray2 = LeakFunctions.GetSizeInfoDictionary((dynamic)LTFunction.SizeInfosBySize);
		Dictionary<double, ILTHeapInfo> dictionary;
		Dictionary<double, ILTHeapInfo> lTHeapInfoArray;
		if (LTFunction.AllocationType == 0)
		{
			dictionary = LeakFunctions.GetLTHeapInfoDictionary((dynamic)LTFunction.LTHeapsByCount);
			lTHeapInfoArray = LeakFunctions.GetLTHeapInfoDictionary((dynamic)LTFunction.LTHeapsBySize);
		}
		else
		{
			dictionary = new Dictionary<double, ILTHeapInfo>();
			lTHeapInfoArray = new Dictionary<double, ILTHeapInfo>();
		}
		if (Globals.Manager.SourceInfoEnabled)
		{
			text = Globals.g_Debugger.GetSourceInfoFromAddress(ReturnAddress);
		}
		Globals.Manager.Write("\t\t<table border=0 cellpadding=0 cellspacing=0 class='myCustomText'>\r\n");
		Globals.Manager.Write("\t\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>Function</td>\r\n");
		if (bHandles)
		{
			Globals.Manager.Write("\t\t\t\t<td><a name='");
			Globals.Manager.Write(GetHandleFunctionLink(ReturnAddress));
			Globals.Manager.Write("'>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(CacheFunctions.GetSymbolFromAddress(ReturnAddress));
			Globals.Manager.Write("</b></a></td>\r\n");
		}
		else
		{
			Globals.Manager.Write("\t\t\t\t<td><a name='");
			Globals.Manager.Write(GetFunctionLink(ReturnAddress));
			Globals.Manager.Write("'>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(CacheFunctions.GetSymbolFromAddress(ReturnAddress));
			Globals.Manager.Write("</b></a></td>\r\n");
		}
		Globals.Manager.Write("\t\t\t</tr>\r\n");
		if (Convert.ToString(text) != "")
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Source Line</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;");
			Globals.Manager.Write(text);
			Globals.Manager.Write("</td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		Globals.Manager.Write("\t\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>Allocation type</td>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(GetAllocType(LTFunction.AllocationType));
		Globals.Manager.Write("</td>\r\n");
		Globals.Manager.Write("\t\t\t</tr>\r\n");
		if (LTFunction.AllocationType == 0 && dictionary.Keys.Count == 1)
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Heap handle</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;");
			Globals.Manager.Write(Globals.HelperFunctions.GetAsHexString(dictionary.Keys.ElementAt(0)));
			Globals.Manager.Write("</td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		if (Convert.ToBoolean(bHandles))
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Handle Count</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(LTFunction.AllocationCount.ToString());
			Globals.Manager.Write(" handle(s)</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		else
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Allocation Count</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(LTFunction.AllocationCount.ToString());
			Globals.Manager.Write(" allocation(s)</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Allocation Size</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(LTFunction.AllocationSize));
			Globals.Manager.Write("</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		Globals.Manager.Write("\t\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>Leak Probability</td>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
		Globals.Manager.Write(Convert.ToString(100.0 - Math.Abs(Convert.ToDouble(LTFunction.LeakProbability))));
		Globals.Manager.Write("%</b></td>\r\n");
		Globals.Manager.Write("\t\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t</table>\r\n");
		Globals.Manager.Write("\t\t<br><br>\r\n");
		if (bHandles)
		{
			PrintSizeInfoGraph(sizeInfoArray, 1);
			PrintSizeInfoGraph(sizeInfoArray2, 0);
			Globals.Manager.Write("<br><br>");
		}
		if (LTFunction.AllocationType == 0 && dictionary.Keys.Count > 1)
		{
			PrintLTHeapGraph(dictionary, 1);
			PrintLTHeapGraph(lTHeapInfoArray, 0);
			Globals.Manager.Write("<br><br>");
		}
		num = LTFunction.CallStackCount;
		Globals.g_bFunctionTracked = true;
		Globals.g_bStackCollected = Globals.g_bStackCollected || num > 1;
		for (num2 = 0; num2 < Convert.ToInt32(num); num2++)
		{
			array = LeakFunctions.GetSynStack((dynamic)LTFunction.get_CallStack(num2));
			Globals.Manager.Write("\t\t\t<p><b>Call stack sample ");
			Globals.Manager.Write((num2 + 1).ToString());
			Globals.Manager.Write("</b></p>\r\n");
			if (LTFunction.AllocationType > 4)
			{
				iLTHandleAllocation = LTFunction.get_HandleAllocation(num2);
				Globals.Manager.Write("\t\t\t<table border=0 cellpadding=0 cellspacing=0 class='myCustomText'>\r\n");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>Handle</td>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>&nbsp;&nbsp;");
				Globals.Manager.Write(Globals.HelperFunctions.GetAsHexString(iLTHandleAllocation.Handle));
				Globals.Manager.Write("</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>Allocation Time</td>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>&nbsp;&nbsp;");
				Globals.Manager.Write(Globals.HelperFunctions.PrintTime(iLTHandleAllocation.AllocationTime));
				Globals.Manager.Write(" since tracking started</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
				Globals.Manager.Write("\t\t\t</table><br><br>\r\n");
			}
			else
			{
				memoryAllocation = LTFunction.get_MemoryAllocation(num2);
				Globals.Manager.Write("\t\t\t<table border=0 cellpadding=0 cellspacing=0 class='myCustomText' ID=\"Table3\">\r\n");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>Address</td>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>&nbsp;&nbsp;");
				Globals.Manager.Write(Globals.HelperFunctions.GetAsHexString(memoryAllocation.AllocationAddress));
				Globals.Manager.Write("</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>Allocation Time</td>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>&nbsp;&nbsp;");
				Globals.Manager.Write(Globals.HelperFunctions.PrintTime(memoryAllocation.AllocationTime));
				Globals.Manager.Write(" since tracking started</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>Allocation Size</td>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td>&nbsp;&nbsp;");
				Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(memoryAllocation.AllocationSize));
				Globals.Manager.Write("</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
				Globals.Manager.Write("\t\t\t</table><br><br>\r\n");
			}
			Globals.Manager.Write("\t\t\t<table border=0 cellpadding=0 cellspacing=0 class=myCustomText ID=\"Table2\">\r\n");
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<th>Function</th>\r\n");
			if (Convert.ToBoolean(Globals.Manager.SourceInfoEnabled))
			{
				Globals.Manager.Write("<th>&nbsp;&nbsp;Source</th>");
			}
			Globals.Manager.Write("\t\t\t\t<th>&nbsp;&nbsp;Destination</th>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
			for (num3 = 0; num3 < array.GetLength(0); num3++)
			{
				text2 = Convert.ToString(array[num3, 0]).Replace("<", "&lt;");
				text2 = text2.Replace(">", "&gt;");
				Globals.Manager.Write("\t\t\t\t<tr>\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td nowrap>");
				Globals.Manager.Write(text2);
				Globals.Manager.Write("</td>\r\n");
				if (Convert.ToBoolean(Globals.Manager.SourceInfoEnabled))
				{
					Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + Convert.ToString(array[num3, 1]) + "</td>");
				}
				Globals.Manager.Write("\t\t\t\t\t\t\t\r\n");
				Globals.Manager.Write("\t\t\t\t\t<td nowrap>&nbsp;&nbsp;");
				Globals.Manager.Write(array[num3, 2]);
				Globals.Manager.Write("</td>\r\n");
				Globals.Manager.Write("\t\t\t\t</tr>\r\n");
			}
			Globals.Manager.Write("\t\t\t</table>\r\n");
			Globals.Manager.Write("\t\t\t<br><br>\r\n");
		}
	}

	private static void PrintLTModuleDetails(double ModuleBase, ILTModule LTModule, int ModuleIndex, bool bHandles)
	{
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		string text = "";
		Dictionary<double, ILTFunction> dictionary = null;
		string[] array = null;
		string text2 = null;
		scriptModuleClass = CacheFunctions.GetModuleFromAddress(ModuleBase);
		if (scriptModuleClass == null)
		{
			array = Globals.HelperFunctions.CheckSymbolType(ModuleBase).Split(':');
			text = ((!(array[1] == "1")) ? ("Unknown module - Base address : " + Globals.HelperFunctions.GetAsHexString(ModuleBase)) : array[0].Split('.')[0]);
		}
		else
		{
			text = "<b>" + Convert.ToString(scriptModuleClass.ModuleName) + "</b>";
		}
		text2 = ((!Convert.ToBoolean(bHandles)) ? GetModuleLink(ModuleBase) : GetHandleModuleLink(ModuleBase));
		Globals.Manager.Write("\t\t<a name='");
		Globals.Manager.Write(text2);
		Globals.Manager.Write("'><h4>Module details for ");
		Globals.Manager.Write(text);
		Globals.Manager.Write("</h4></a>\r\n");
		Globals.Manager.Write("\t\t<table border=0 cellpadding=0 cellspacing=0 class='myCustomText'>\r\n");
		Globals.Manager.Write("\t\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>Module Name</td>\r\n");
		Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
		Globals.Manager.Write(text);
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t\t</tr>\r\n");
		if (Convert.ToBoolean(bHandles))
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Handle Count</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(LTModule.AllocationCount.ToString());
			Globals.Manager.Write(" handle(s)</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		else
		{
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Allocation Count</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(LTModule.AllocationCount.ToString());
			Globals.Manager.Write(" allocation(s)</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
			Globals.Manager.Write("\t\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>Allocation Size</td>\r\n");
			Globals.Manager.Write("\t\t\t\t<td>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize));
			Globals.Manager.Write("</b></td>\r\n");
			Globals.Manager.Write("\t\t\t</tr>\r\n");
		}
		Globals.Manager.Write("\t\t</table>\r\n");
		Globals.Manager.Write("\t\t<br>\r\n");
		if (scriptModuleClass != null)
		{
			Globals.HelperFunctions.ModuleInfo(scriptModuleClass);
		}
		if (ModuleIndex != Globals.TopModuleLimit)
		{
			dictionary = LeakFunctions.GetLTFunctionDictionary((dynamic)LTModule.FunctionsByCount);
			PrintFunctionGraph(dictionary, 1, bHandles);
			Globals.Manager.Write("<br><br>");
			if (Convert.ToBoolean(bHandles))
			{
				PrintLTTypeGraph(LeakFunctions.GetLTTypeDictionary((dynamic)LTModule.LTTypesByCount), 1, bHandles);
			}
			else
			{
				dictionary = LeakFunctions.GetLTFunctionDictionary((dynamic)LTModule.FunctionsBySize);
				PrintFunctionGraph(dictionary, 0, bHandles);
			}
			Globals.Manager.Write("<br><br><h5>Function details</h5>");
			if (Globals.TopNumFunctionsLimit >= dictionary.Keys.Count)
			{
				_ = dictionary.Keys.Count;
			}
			else
			{
				_ = Globals.TopNumFunctionsLimit;
			}
			foreach (KeyValuePair<double, ILTFunction> item in dictionary)
			{
				PrintLTFunctionDetails(item.Key, item.Value, bHandles);
			}
		}
		Globals.Manager.Write("<a href='#'>Back to Top</a>");
	}

	private static string GetLTVersion()
	{
		string text = "";
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		scriptModuleClass = Globals.g_ModuleCache.GetModuleByName("LeakTrack");
		if (scriptModuleClass == null)
		{
			return "0.0.0.0";
		}
		scriptModuleClass.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
		return Convert.ToString(Major) + "." + Convert.ToString(Minor) + "." + Convert.ToString(Build) + "." + Convert.ToString(Priv);
	}

	private static bool IsLowerLTVersion()
	{
		bool flag = false;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		flag = false;
		scriptModuleClass = Globals.g_ModuleCache.GetModuleByName("LeakTrack");
		if (scriptModuleClass != null)
		{
			scriptModuleClass.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
			if ((Convert.ToInt32(Major) == 1 && Convert.ToInt32(Minor) == 1) || (Convert.ToInt32(Major) == 1 && Convert.ToInt32(Minor) == 0))
			{
				flag = true;
			}
		}
		return flag;
	}

	private static void ScanForKnownLeaks(ILTModule LTModule, double ModuleBase, string ModInfo)
	{
		double num = 0.0;
		ILTFunction iLTFunction = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		dynamic val = null;
		string text = "";
		int num5 = 0;
		string text2 = "";
		string text3 = "";
		IASPInfo iASPInfo = null;
		string[] array = null;
		dynamic val2 = null;
		int num6 = 0;
		string text4 = "";
		string text5 = "";
		Globals.g_Weight--;
		array = ModInfo.Split(':');
		scriptModuleClass = CacheFunctions.GetModuleFromAddress(ModuleBase);
		_ = array[1];
		if (array[2] == "0" && !(array[0] == "UNKNOWN_MODULE"))
		{
			text = "<b>WARNING</b> - DebugDiag was not able to locate debug symbols for <b>" + array[0] + "</b>, so the reported function name(s) may not be accurate.<br><br>";
		}
		val = LTModule.FunctionsBySize;
		num = val[0, 0];
		iLTFunction = val[0, 1];
		text3 = CacheFunctions.GetSymbolFromAddress(num).ToUpper();
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		switch (text3)
		{
		case "MSADO15!CSYSSTRING::CSYSSTRING+25":
			text = "<a href='" + GetModuleLink(ModuleBase) + "'><b>" + array[0] + "</b></a> is responsible for <b>" + Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize) + "</b> worth of outstanding allocations. <br><br>This may be due to a known leak in ADO caused when passing an ADO 'Field' object as a parameter value to the <B>Command.CreateParameter</B> method.";
			text2 = "Please see KB article <b><a href='http://support.microsoft.com/?id=817518'>Q817518</a></b> for more information, including a possible workaround.";
			if (Convert.ToInt32(dumpFiles.Count) > 0)
			{
				text = text + "<br><br>This was detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>";
			}
			Globals.Manager.ReportError(text, text2, 1000, "{12ddfbf6-9e30-47a0-9ba7-b2747a45d108}");
			return;
		case "ASP!CTEMPLATE::LARGEREALLOC+1C":
		case "ASP!CTEMPLATE::LARGEMALLOC+18":
			iASPInfo = Globals.g_ASPInfo;
			text = "Memory allocations by the <a href='" + GetModuleLink(ModuleBase) + "'><b>" + array[0] + "</b></a> template cache Globals.Manager are responsible for <b>" + Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize) + "</b> worth of outstanding allocations. <br><br>Large amounts of memory allocated for the ASP Template Cache are usually caused by the usage of too much VBScript or JScript within a page (or it's associated include files), and may result in degraded ASP performance due to heap fragmentation or memory consumption by the host process.<br><br><b>ASP Template Cache Size: " + Convert.ToString(Globals.ReportASPInfo.GetTemplateCacheSize(iASPInfo)) + "</b>";
			text2 = "Please see the following whitepaper for recommendations on ASP performance tuning:<br><br><b><a href='http://msdn.microsoft.com/library/default.aspx?url=/library/en-us/dnasp/html/asptips.aspx'>25+ ASP Tips to Improve Performance and Style</a></b>";
			if (Convert.ToInt32(dumpFiles.Count) > 0)
			{
				text = text + "<br><br>This was detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>";
			}
			Globals.Manager.ReportWarning(text, text2, 1000, "{fac93bd9-6abf-4719-b952-bb464ad52cf6}");
			return;
		}
		num6 = iLTFunction.CallStackCount;
		if (scriptModuleClass == null)
		{
			text = text + "<a href='" + GetModuleLink(ModuleBase) + "'><b>" + array[0] + "</b></a> is responsible for <b>" + Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize) + "</b> worth of outstanding allocations. The following are the top " + Convert.ToString(Globals.TopLeakyFunctionLimit) + " memory consuming functions:<br>";
			num5 = ((Globals.HelperFunctions.UBound_HACK_DO_NOT_USE((Array)val, 1) >= Globals.TopLeakyFunctionLimit) ? Globals.TopLeakyFunctionLimit : (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE((Array)val, 1) + 1));
			for (num2 = 0; num2 <= num5 - 1; num2++)
			{
				num = val[num2, 0];
				iLTFunction = val[num2, 1];
				text = text + "<br><a href='" + GetFunctionLink(num) + "'><b>" + CacheFunctions.GetSymbolFromAddress(num) + "</b></a>:  <b>" + Globals.HelperFunctions.PrintMemory(iLTFunction.AllocationSize) + "</b> worth of outstanding allocations.";
				text2 = "If this is unexpected, please contact the vendor of this module";
				text2 += " for further assistance with this issue.";
			}
		}
		else if (Globals.UseKnownGoodModList && IsKnownGoodModule(scriptModuleClass.ImageName) && Convert.ToInt32(num6) > 0)
		{
			text = text + "<a href='" + GetModuleLink(ModuleBase) + "'><b>" + array[0] + "</b></a> (a known Windows memory Globals.Manager) is responsible for <b>" + Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize) + "</b> worth of outstanding allocations. These allocations appear to have originated from the following module(s) and function(s):<br><br>";
			num2 = 0;
			text5 = ";";
			for (num3 = 0; num3 <= Convert.ToInt32(iLTFunction.CallStackCount) - 1; num3++)
			{
				val2 = iLTFunction.get_CallStack(num3);
				for (num4 = 0; num4 <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE((Array)val2, 1); num4++)
				{
					text4 = Convert.ToString(val2[num4, 0]).Replace("<", "&lt");
					text4 = text4.Replace(">", "&gt");
					array = text4.Split('!');
					if (!IsKnownGoodModule(array[0]))
					{
						if (text5.IndexOf(text4, 0) == -1)
						{
							num2++;
							text = text + "<Font Color='Red'><b>" + text4 + "</b></Font><br>";
							text5 = text5 + text4 + ";";
						}
						break;
					}
				}
			}
			if (num2 == 0)
			{
				text = text + "No module info available. Please see the <a href='" + GetFunctionLink(num) + "'>callstack samples</a> for further information on these allocations.";
			}
			text += "<br><br>";
			text2 = "Review the <a href='" + GetFunctionLink(num) + "'>callstack samples</a> below for further information. If this is unexpected, please contact the vendor of the calling module for further assistance";
		}
		else
		{
			text = text + "<a href='" + GetModuleLink(ModuleBase) + "'><b>" + array[0] + "</b></a> is responsible for <b>" + Globals.HelperFunctions.PrintMemory(LTModule.AllocationSize) + "</b> worth of outstanding allocations. The following are the top " + Convert.ToString(Globals.TopLeakyFunctionLimit) + " memory consuming functions:<br>";
			num5 = ((Globals.HelperFunctions.UBound_HACK_DO_NOT_USE((Array)val, 1) >= Globals.TopLeakyFunctionLimit) ? Globals.TopLeakyFunctionLimit : (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE((Array)val, 1) + 1));
			for (num2 = 0; num2 <= num5 - 1; num2++)
			{
				num = val[num2, 0];
				iLTFunction = val[num2, 1];
				text = text + "<br><a href='" + GetFunctionLink(num) + "'><b>" + CacheFunctions.GetSymbolFromAddress(num) + "</b></a>:  <b>" + Globals.HelperFunctions.PrintMemory(iLTFunction.AllocationSize) + "</b> worth of outstanding allocations.";
				text2 = "If this is unexpected, please contact the vendor of this module";
				if (Convert.ToString(scriptModuleClass.VSCompanyName) != "")
				{
					text2 = text2 + ", <b>" + Convert.ToString(scriptModuleClass.VSCompanyName) + "</b>,";
				}
				text2 += " for further assistance with this issue.";
			}
		}
		if (Convert.ToInt32(dumpFiles.Count) > 1)
		{
			text = text + "<br><br>This was detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>";
		}
		Globals.Manager.ReportWarning(text, text2, Globals.g_Weight, "{d84d813f-74c4-4823-ac81-bc3421c6210b}");
	}

	private static bool IsKnownGoodModule(string ModuleName)
	{
		string text = "";
		int num = 0;
		int num2 = 0;
		string[] array = null;
		text = "KERNEL32;NTDLL;MSVCRT;OLE32;COMBASE;OLEAUT32;MSDART;MSXML30;MSXML40;MSCORWKS;MSCORSVR;MSDATL3:LEAKTRACK:MSVCR70:MSVCR71:MSVCR80:MSVCR90:MSVCR100:MSVCR70D:MSVCR71:MSVCR80D:MSVCR90D:MSVCR100D";
		num = ModuleName.LastIndexOf("\\", 0, StringComparison.CurrentCultureIgnoreCase);
		num2 = ModuleName.GetSafeLength();
		array = ModuleName.Substring(ModuleName.GetSafeLength() - num2 - num).Split('.');
		if (text.IndexOf(array[0].ToString().ToUpper(), 0) >= 0)
		{
			return true;
		}
		return false;
	}

	private static string FormatNumber(double value, int index)
	{
		string empty = string.Empty;
		return index switch
		{
			0 => $"{value:0,0}", 
			1 => $"{value:0,0.0}", 
			2 => $"{value:0,0.00}", 
			_ => value.ToString(), 
		};
	}

	private string[] Split(string Expression, string Delimiter, int Count, CompareMethod CompareMode)
	{
		return Globals.HelperFunctions.Split(Expression, Delimiter, Count, 0);
	}

	private string StringReplace(string stringToPass, string find, string replaceWith, CompareMethod CompareMode)
	{
		return Globals.HelperFunctions.Replace(stringToPass, find, replaceWith);
	}

	private string StringReplace(string Expression, string Find, string Replacement, int Start, int Count, CompareMethod CompareMode)
	{
		return Globals.HelperFunctions.Replace(Expression, Find, Replacement);
	}
}
