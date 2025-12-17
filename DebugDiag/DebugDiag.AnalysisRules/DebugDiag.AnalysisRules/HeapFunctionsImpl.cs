using System;
using System.Collections.Generic;
using DebugDiag.DotNet;
using DebugDiag.DotNet.Reports;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

public class HeapFunctionsImpl : IHeapFunctions
{
	private NetScriptManager Manager = Globals.Manager;

	public const int PageSize = 4096;

	public const int HeapStepCount = 4;

	public static int TopStatLimit;

	public const int FragThreshold = 75;

	public int I;

	public int TopStatCount;

	public int TopHeapCount;

	public double TotalReserve;

	public double TotalCommit;

	public double SummaryCount;

	public string Message;

	public string Recommendation;

	static HeapFunctionsImpl()
	{
		TopStatLimit = 10;
		if (!int.TryParse(ConfigHelper.GetSetting("TopStatLimit"), out TopStatLimit))
		{
			TopStatLimit = 10;
		}
	}

	public void AnalyzeAndReportHeapInfo()
	{
		SummaryCount = Globals.Manager.GetResults(0).Count;
		ReportSection val = Globals.Manager.AddReportSection("HeapReport", (SectionType)0);
		val.Title = "Heap Analysis";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		Globals.g_Progress.CurrentStatus = "Loading heap information. Please wait...";
		Globals.g_Progress.SetCurrentRange(0, 4);
		object[] array = (object[])Globals.g_HeapInfo.SortHeapsByReserved();
		List<INTHeap2> list = new List<INTHeap2>();
		Globals.g_HeapArray = new INTHeap[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			INTHeap2 iNTHeap = (INTHeap2)array[i];
            if (iNTHeap != null)
			{
				string heapType = iNTHeap.HeapType;
				if (!string.IsNullOrEmpty(heapType) && heapType.Equals("NT", StringComparison.CurrentCultureIgnoreCase))
				{
					list.Add(iNTHeap);
				}
				Globals.g_HeapArray[i] = (INTHeap)iNTHeap;
			}
		}
		Globals.g_NTHeapArray = list.ToArray();
		for (I = 0; I <= Globals.g_HeapInfo.Count - 1; I++)
		{
			TotalReserve += Globals.g_HeapArray[I].ReservedMemory;
			TotalCommit += Globals.g_HeapArray[I].CommittedMemory;
		}
		ReportSection val2 = val.AddChildSection("HeapSummary", (SectionType)0);
		val2.Title = "Heap Summary";
		Globals.Manager.CurrentSection = val2;
		Globals.Manager.Write("<table class=myCustomText ID=\"Table3\">");
		Globals.Manager.Write("<tr>");
		Globals.Manager.Write("<td><b>Number of heaps</b></td>");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.g_HeapInfo.Count + " Heaps</b></td>");
		Globals.Manager.Write("</tr>");
		Globals.Manager.Write("<tr>");
		Globals.Manager.Write("<td><b>Number of NT heaps</b></td>");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + list.Count + " Heaps</b></td>");
		Globals.Manager.Write("</tr>");
		Globals.Manager.Write("<tr>");
		Globals.Manager.Write("<td><b>Total reserved memory</b></td>");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(TotalReserve) + "</b></td>");
		Globals.Manager.Write("</tr>");
		Globals.Manager.Write("<tr>");
		Globals.Manager.Write("<td><b>Total committed memory</b></td>");
		Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(TotalCommit) + "</b></td>");
		Globals.Manager.Write("</tr>");
		Globals.Manager.Write("</table>");
		if (Globals.g_NTHeapArray.Length > Globals.TopHeapLimit)
		{
			TopHeapCount = Globals.TopHeapLimit;
		}
		else
		{
			TopHeapCount = Globals.g_NTHeapArray.Length;
		}
		Globals.g_Progress.CurrentPosition = 1;
		val2 = val.AddChildSection("TopReserved", (SectionType)0);
		val2.Title = "Top " + TopHeapCount + " heaps by reserved memory";
		Globals.Manager.CurrentSection = val2;
		Globals.g_HeapGraph = new BarGraph();
		Globals.g_HeapGraph.SetRowCount(Convert.ToInt32(TopHeapCount));
		for (I = 0; I <= TopHeapCount - 1; I++)
		{
			Globals.g_HeapGraph.Rows[I].Value = ((INTHeap)Globals.g_NTHeapArray[I]).ReservedMemory;
			Globals.g_HeapGraph.Rows[I].Caption = Globals.g_Debugger.GetAs32BitHexString(((INTHeap)Globals.g_NTHeapArray[I]).Handle);
			Globals.g_HeapGraph.Rows[I].Caption2 = Globals.HelperFunctions.PrintMemory(((INTHeap)Globals.g_NTHeapArray[I]).ReservedMemory);
			Globals.g_HeapGraph.Rows[I].Link = GetHeapLink((INTHeap)Globals.g_NTHeapArray[I]);
		}
		Globals.g_HeapGraph.DrawGraph();
		Globals.g_Progress.CurrentPosition = 2;
		Globals.g_Progress.CurrentStatus = "Sorting heaps by committed memory";
		array = (object[])Globals.g_HeapInfo.SortHeapsByCommitted();
		list.Clear();
		Globals.g_HeapArray = new INTHeap[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			Globals.g_HeapArray[j] = (INTHeap)array[j];
			INTHeap2 iNTHeap2 = (INTHeap2)array[j];
			if (iNTHeap2 != null)
			{
				string heapType2 = iNTHeap2.HeapType;
				if (!string.IsNullOrEmpty(heapType2) && heapType2.Equals("NT", StringComparison.CurrentCultureIgnoreCase))
				{
					list.Add(iNTHeap2);
				}
			}
		}
		Globals.g_NTHeapArray = list.ToArray();
		val2 = val.AddChildSection("TopComitted", (SectionType)0);
		val2.Title = "Top " + TopHeapCount + " heaps by committed memory";
		Globals.Manager.CurrentSection = val2;
		Globals.g_HeapGraph = new BarGraph();
		Globals.g_HeapGraph.SetRowCount(Convert.ToInt32(TopHeapCount));
		for (I = 0; I <= TopHeapCount - 1; I++)
		{
			Globals.g_HeapGraph.Rows[I].Value = ((INTHeap)Globals.g_NTHeapArray[I]).CommittedMemory;
			Globals.g_HeapGraph.Rows[I].Caption = Globals.g_Debugger.GetAs32BitHexString(((INTHeap)Globals.g_NTHeapArray[I]).Handle);
			Globals.g_HeapGraph.Rows[I].Caption2 = Globals.HelperFunctions.PrintMemory(((INTHeap)Globals.g_NTHeapArray[I]).CommittedMemory);
			Globals.g_HeapGraph.Rows[I].Link = GetHeapLink((INTHeap)Globals.g_NTHeapArray[I]);
		}
		Globals.g_HeapGraph.DrawGraph();
		Globals.g_Progress.CurrentPosition = 3;
		Globals.g_Progress.CurrentStatus = "Generating detailed heap report";
		Globals.g_Progress.SetCurrentRange(0, Globals.g_HeapInfo.Count);
		val2 = val.AddChildSection("HeapDetails", (SectionType)0);
		val2.Title = "Heap Details";
		Globals.Manager.CurrentSection = val2;
		Globals.HighFragHeaps = "";
		foreach (INTHeap item in Globals.g_HeapInfo)
		{
			PrintHeapInfo(item);
			Globals.g_Progress.CurrentPosition = item.Index + 1;
		}
		if (Globals.g_OSVER < Globals.OS_VER_WINVISTA && Globals.HighFragHeaps != "")
		{
			Message = "Detected symptoms of high fragmentation in the following heaps in " + Globals.g_ShortDumpFileName + "<br><br>" + Globals.HighFragHeaps;
			Globals.Manager.ReportWarning(Message, "Heap fragmentation is often caused by one of the following two reasons<br><br>1. Small heap memory blocks that are leaked (allocated but never freed) over time<br>2. Mixing long lived small allocations with short lived long allocations<br><br>Both of these reasons can prevent the NT heap manager from using free memory effeciently since they are  spread as small fragments that cannot be used as a single large allocation", 0, "{f0c39ddc-f5dd-4a12-a686-8fe30247dd8a}");
		}
		Globals.g_Progress.CurrentPosition = 4;
		Globals.Manager.CurrentSection = val.Parent;
	}

	public void ShowHeapInfoNoneDetectedIfNecessary()
	{
		if (SummaryCount == Convert.ToDouble(Globals.Manager.GetResults(0).Count))
		{
			Globals.Manager.ReportOther("<br>DebugDiag did not detect any known <b>native heap(unmanaged)</b> problems in " + Globals.g_ShortDumpFileName + " using the current set of rules.<br><br>", "", "Notification", "notificationicon.png", 0, "{bb643989-7d32-400f-88b2-bcfee03490bd}");
		}
	}

	public void PrintHeapInfo(INTHeap Heap)
	{
		string text = "";
		double num = Heap.ReservedMemory - Heap.CommittedMemory;
		double num2 = ((!(num > 0.0)) ? 0.0 : ((num - Heap.LargestUnCommittedRange) / num * 100.0));
		if (num2 > 75.0)
		{
			Globals.HighFragHeaps = Globals.HighFragHeaps + "<br><a href='" + GetHeapLink(Heap) + "'><b>" + Globals.g_Debugger.GetAs32BitHexString(Heap.Handle) + "</b></a> (";
			if (!string.IsNullOrEmpty(Heap.Name))
			{
				Globals.HighFragHeaps = Globals.HighFragHeaps + "<b>" + Heap.Name + "</b> - ";
				if (Globals.HelperFunctions.InStr(Globals.HelperFunctions.UCase(Heap.Name), "ASP!CTEMPLATE") > 0)
				{
					int num3 = Globals.ReportASPInfo.FileWithMorethan10Includes();
					if (num3 > 0)
					{
						Manager.ReportWarning("<b>" + num3 + " ASP Template file(s) </b> are including more than 10 include files. The size of the ASP template cache increases significantly when additional ASP files and a potentially large number of include files are  added to the cache. You may also notice slow performance and increased memory usage on the Web server.", "For more details on this issue refer  to <b><a href='http://support.microsoft.com/kb/914156'>http://support.microsoft.com/kb/914156</a></b>", 0, "{af85011e-f10a-4181-ba7b-4e2843ca8f22}");
					}
				}
			}
			Globals.HighFragHeaps = Globals.HighFragHeaps + Globals.HelperFunctions.FormatNumber(num2, 2) + "% Fragmented)";
		}
		string name = Heap.Name;
		Globals.Manager.Write("<h4>");
		Globals.Manager.Write("<a name='#" + Globals.g_UniqueReference + Heap.Handle + "'>");
		INTHeap2 heap2 = Heap as INTHeap2;
		string heapType = heap2?.HeapType ?? "Unknown";
		Globals.Manager.Write("Heap " + Heap.Index + " - " + Globals.g_Debugger.GetAs32BitHexString(Heap.Handle) + " | " + heapType + " Heap");
		Globals.Manager.Write("</a>");
		Globals.Manager.Write("</h4>");
		if (!string.IsNullOrEmpty(heapType) && heapType.Equals("NT", StringComparison.CurrentCultureIgnoreCase))
		{
			Globals.Manager.Write("<table class=myCustomText ID=\"Table1\">");
			if (name != "")
			{
				Globals.Manager.Write("<tr>");
				Globals.Manager.Write("<td><b>Heap Name</b></td>");
				Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + name + "</b></td>");
				Globals.Manager.Write("</tr>");
				Globals.Manager.Write("<tr>");
				Globals.Manager.Write("<td><b>Heap Description</b></td>");
				Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Heap.Description + "</b></td>");
				Globals.Manager.Write("</tr>");
			}
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td><b>Reserved memory</b></td>");
			Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(Heap.ReservedMemory) + "</b></td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td><b>Committed memory</b></td>");
			Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(Heap.CommittedMemory));
			Globals.Manager.Write("(" + Globals.HelperFunctions.FormatNumber(Heap.CommittedMemory / Heap.ReservedMemory * 100.0, 2) + "% of reserved)");
			Globals.Manager.Write("</b></td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td><b>Uncommitted memory</b></td>");
			Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(num));
			Globals.Manager.Write("(" + Globals.HelperFunctions.FormatNumber(num / Heap.ReservedMemory * 100.0, 2) + "% of reserved)");
			Globals.Manager.Write("</b></td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td><b>Number of heap segments</b></td>");
			Globals.Manager.Write("<td><b>&nbsp;&nbsp;" + Heap.Count + " segments</b></td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td>Number of uncommitted ranges</td>");
			Globals.Manager.Write("<td>&nbsp;&nbsp;" + Heap.NumberOfUnCommittedRanges + " range(s)</td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td>Size of largest uncommitted range</td>");
			Globals.Manager.Write("<td>&nbsp;&nbsp;" + Globals.HelperFunctions.PrintMemory(Heap.LargestUnCommittedRange) + "</td>");
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("<tr>");
			if (Globals.g_OSVER < Globals.OS_VER_WINVISTA)
			{
				Globals.Manager.Write("<td>Calculated heap fragmentation</td>");
				Globals.Manager.Write("<td>&nbsp;&nbsp;" + Globals.HelperFunctions.FormatNumber(num2, 2) + "</td>");
			}
			else
			{
				Globals.Manager.Write("<td>Calculated heap fragmentation</td>");
				Globals.Manager.Write("<td>Unavailable</td>");
			}
			Globals.Manager.Write("</tr>");
			Globals.Manager.Write("</table>");
			Globals.Manager.Write("<br><br>");
			Globals.Manager.Write("<h5>Segment Information</h5>");
			Globals.Manager.Write("<table class=myCustomText cellspacing=3 ID=\"Table2\">");
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<th>Base Address</th>");
			Globals.Manager.Write("<th>Reserved Size</th>");
			Globals.Manager.Write("<th>Committed Size</th>");
			Globals.Manager.Write("<th>Uncommitted Size</th>");
			Globals.Manager.Write("<th>Number of uncommitted ranges</th>");
			Globals.Manager.Write("<th>Largest uncommitted block</th>");
			Globals.Manager.Write("<th>Calculated heap fragmentation</th>");
			Globals.Manager.Write("</tr>");
			if (Heap.Count > 63 && Globals.HelperFunctions.FormatNumber(Heap.CommittedMemory / Heap.ReservedMemory * 100.0, 2) < 10.0 && Globals.HelperFunctions.Not(Globals.HelperFunctions.UCase(name) == "DEFAULT PROCESS HEAP"))
			{
				string text2 = "In <b>" + Globals.g_ShortDumpFileName + "</b>, heap <a href='" + GetHeapLink(Heap) + "'>" + Globals.g_Debugger.GetAs32BitHexString(Heap.Handle) + "</a> contains <b>" + Heap.Count + "</b> segments, with very little committed memory in the heap. Any allocation request for this heap greater than <b>" + Globals.HelperFunctions.PrintMemory(Heap.LargestUnCommittedRange) + "</b> will fail.<br><br>Memory statistics for this heap:<br><br>Reserved Memory: <b>" + Globals.HelperFunctions.PrintMemory(Heap.ReservedMemory) + "</b><br>Committed Memory: <b>" + Globals.HelperFunctions.PrintMemory(Heap.CommittedMemory) + "</b> (" + Globals.HelperFunctions.FormatNumber(Heap.CommittedMemory / Heap.ReservedMemory * 100.0, 2) + "% of reserved)";
				if (name != "")
				{
					text2 = text2 + "<br>Heap Name: <b>" + name + "</b>";
					text = GetHeapOwnerModule(name);
				}
				string text3 = "Check with the heap creator ";
				if (name.Equals(string.Empty) || text == "<Unable to obtain heap owner data>")
				{
					text3 += "(unable to obtain heap owner data for this heap), ";
				}
				else
				{
					CacheFunctions.ScriptModuleClass moduleByName = Globals.g_ModuleCache.GetModuleByName(text);
					text3 = ((!Globals.HelperFunctions.Not(moduleByName == null)) ? (text3 + "(created by: <b>" + text + "</b>), ") : (text3 + "(<b>" + moduleByName.VSCompanyName + "</b>), "));
				}
				text3 += "and ensure the heap is destroyed when all outstanding allocations have been freed. <br><br>If there are outstanding allocations, review the allocations to see if a leak is preventing this heap from being destroyed.";
				Manager.ReportWarning(text2, text3, 0, "{de0b75b9-de5f-4d45-b84d-1176af3bca87}");
			}
			foreach (IHeapSegment item in Heap)
			{
				double num4 = item.NumberOfPages * 4096.0;
				num = item.NumberOfUnCommittedPages * 4096.0;
				double memory = num4 - num;
				double largestUnCommittedRange = item.LargestUnCommittedRange;
				num2 = ((!(num > 0.0)) ? 0.0 : ((Heap.LargestUnCommittedRange != 0.0) ? ((num - Heap.LargestUnCommittedRange) / num * 100.0) : (-1.0)));
				Globals.Manager.Write("<tr>");
				Globals.Manager.Write("<td>" + Globals.g_Debugger.GetAs32BitHexString(item.BaseAddress) + "</td>");
				Globals.Manager.Write("<td>" + Globals.HelperFunctions.PrintMemory(num4) + "</td>");
				Globals.Manager.Write("<td>" + Globals.HelperFunctions.PrintMemory(memory) + "</td>");
				Globals.Manager.Write("<td>" + Globals.HelperFunctions.PrintMemory(num) + "</td>");
				Globals.Manager.Write("<td>" + item.NumberOfUnCommittedRanges + "</td>");
				Globals.Manager.Write("<td>" + Globals.HelperFunctions.PrintMemory(largestUnCommittedRange) + "</td>");
				Globals.Manager.Write("<td>" + Globals.HelperFunctions.FormatNumber(num2, 2) + "%</td>");
				if (num2 > -1.0)
				{
					Globals.Manager.Write("<td>" + Globals.HelperFunctions.FormatNumber(num2, 2) + "</td>");
				}
				else
				{
					Globals.Manager.Write("<td>Unavailable</td>");
				}
				Globals.Manager.Write("</tr>");
			}
			Globals.Manager.Write("</table>");
			Globals.Manager.Write("<br><br>");
			object[,] array = (object[,])Heap.BusyStatisticsBySize;
			double[,] array2 = new double[array.GetLength(0), array.GetLength(1)];
			for (int i = 0; i < array.GetLength(0); i++)
			{
				for (int j = 0; j < array.GetLength(1); j++)
				{
					array2[i, j] = (double)array[i, j];
				}
			}
			if (Globals.HelperFunctions.UBound(array2) > TopStatLimit)
			{
				TopStatCount = TopStatLimit;
			}
			else
			{
				TopStatCount = Globals.HelperFunctions.UBound(array2) + 1;
			}
			if (TopStatCount > 0)
			{
				Globals.Manager.Write("<h5>Top " + TopStatCount + " allocations by size</h5><br>");
				BarGraph barGraph = new BarGraph();
				barGraph.SetRowCount(TopStatCount);
				for (int k = 0; k <= TopStatCount - 1; k++)
				{
					barGraph.Rows[k].Value = array2[k, 0] * array2[k, 1];
					barGraph.Rows[k].Caption = "Allocation Size - " + array2[k, 0];
					barGraph.Rows[k].Caption2 = Globals.HelperFunctions.PrintMemory(array2[k, 0] * array2[k, 1]);
				}
				barGraph.DrawGraph();
			}
			array = (object[,])Heap.BusyStatisticsByCount;
			array2 = new double[array.GetLength(0), array.GetLength(1)];
			for (int l = 0; l < array.GetLength(0); l++)
			{
				for (int m = 0; m < array.GetLength(1); m++)
				{
					array2[l, m] = (double)array[l, m];
				}
			}
			if (Globals.HelperFunctions.UBound(array2) > TopStatLimit)
			{
				TopStatCount = TopStatLimit;
			}
			else
			{
				TopStatCount = Globals.HelperFunctions.UBound(array2) + 1;
			}
			if (TopStatCount > 0)
			{
				Globals.Manager.Write("<h5>Top " + TopStatCount + " allocations by count</h5><br>");
				BarGraph barGraph2 = new BarGraph();
				barGraph2.SetRowCount(TopStatCount);
				for (int n = 0; n <= TopStatCount - 1; n++)
				{
					barGraph2.Rows[n].Value = array2[n, 1];
					barGraph2.Rows[n].Caption = "Allocation Size - " + array2[n, 0];
					barGraph2.Rows[n].Caption2 = array2[n, 1] + " allocation(s)";
				}
				barGraph2.DrawGraph();
			}
		}
		Globals.Manager.Write("<a href='#'>Back to Top</a>");
		Globals.Manager.Write("<br><br><br><br>");
	}

	public string GetHeapLink(INTHeap Heap)
	{
		return "#" + Globals.g_UniqueReference + Convert.ToString(Heap.Handle);
	}

	public bool IsHeapFunction(double Address)
	{
		string functionName = CacheFunctions.GetFunctionName(Address);
		if (string.IsNullOrEmpty(functionName))
		{
			return false;
		}
		functionName = functionName.ToUpper();
		if (functionName.StartsWith("NTDLL!RTL") && functionName.Contains("HEAP"))
		{
			return true;
		}
		string[] array = Globals.HelperFunctions.Split(functionName, "+", -1);
		if (array[0] == "NTDLL!RTLPCOALESCEFREEBLOCKS" || array[0] == "NTDLL!RTLPINTERLOCKEDPOPENTRYSLIST" || array[0] == "NTDLL!EXPINTERLOCKEDPOPENTRYSLISTFAULT" || array[0] == "NTDLL!RTLREPORTCRITICALFAILURE" || array[0] == "NTDLL!RTLPDPHREPORTCORRUPTEDBLOCK" || array[0] == "NTDLL!RTLPFINDANDCOMMITPAGES")
		{
			return true;
		}
		return false;
	}

	public CacheFunctions.ScriptModuleClass AnalyzeHeapCorruption(CacheFunctions.ScriptThreadClass ExceptionThread)
	{
		string findIn = "NTDLL!RTLFREEHEAP;NTDLL!RTLALLOCATEHEAP;NTDLL!RTLPCOALESCEFREEBLOCKS;NTDLL!RTLPINTERLOCKEDPOPENTRYSLIST;NTDLL!RTLPINTERLOCKEDPOPENTRYSLIST;NTDLL!RTLPALLOCATEFROMHEAPLOOKASIDE;NTDLL!RTLPDPHREPORTCORRUPTEDBLOCK;NTDLL!RTLPBREAKPOINTHEAP;NTDLL!RTLPDPHISNORMALHEAPBLOCK;NTDLL!RTLPDPHNORMALHEAPFREE;NTDLL!RTLPDEBUGPAGEHEAPFREE;NTDLL!RTLDEBUGFREEHEAP;NTDLL!RTLFREEHEAPSLOWLY;MSVCRT!FREE";
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = ExceptionThread.StackFrames;
		for (int i = 0; i <= stackFrames.Count - 1; i++)
		{
			CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = stackFrames[i];
			string frameText = scriptStackFrameClass.GetFrameText();
			if (Globals.HelperFunctions.InStr(1, findIn, Globals.HelperFunctions.UCase(frameText)) == 0)
			{
				return CacheFunctions.GetModuleFromAddress(scriptStackFrameClass.InstructionAddress);
			}
		}
		return null;
	}

	public string GetHeapOwnerModule(string HeapName)
	{
		if (Globals.HelperFunctions.InStr(1, Globals.HelperFunctions.UCase(HeapName), "MICROSOFT DATA ACCESS RUNTIME MPHEAP") > 0)
		{
			return "msado15";
		}
		if (Globals.HelperFunctions.InStr(1, Globals.HelperFunctions.UCase(HeapName), "ODBC32 MPHEAP") > 0)
		{
			return "odbc32";
		}
		if (Globals.HelperFunctions.InStr(1, Globals.HelperFunctions.UCase(HeapName), "MICROSOFT XML PARSER V3 MPHEAP") > 0)
		{
			return "MSXML3";
		}
		if (Globals.HelperFunctions.InStr(1, Globals.HelperFunctions.UCase(HeapName), "!") > 0)
		{
			return Globals.HelperFunctions.Split(Globals.HelperFunctions.UCase(HeapName), "!")[0];
		}
		return "<Unable to obtain heap owner data>";
	}

	public string GetGlobalFlagDescription(int GFValue)
	{
		string text = "";
		Dictionary<long, string> dictionary = new Dictionary<long, string>();
		dictionary.Add(1L, "Stop on exception");
		dictionary.Add(2L, "Show loader snaps");
		dictionary.Add(4L, "Debug initial command");
		dictionary.Add(8L, "Stop on a hung GUI");
		dictionary.Add(16L, "Enable heap tail checking");
		dictionary.Add(32L, "Enable heap free checking");
		dictionary.Add(64L, "Enable heap parameter checking");
		dictionary.Add(128L, "Enable heap validation on call");
		dictionary.Add(256L, "Enable pool tail checking");
		dictionary.Add(512L, "Enable pool free checking");
		dictionary.Add(1024L, "Enable pool tagging");
		dictionary.Add(2048L, "Enable heap tagging");
		dictionary.Add(4096L, "Create user-mode stack trace DB");
		dictionary.Add(8192L, "Create kernel-mode stack trace DB");
		dictionary.Add(16384L, "Maintain a list of objects for each type");
		dictionary.Add(32768L, "Enable heap tagging by DLL");
		dictionary.Add(131072L, "Enable debugging of Microsoft\ufffd Win32\ufffd subsystem");
		dictionary.Add(262144L, "Enable loading of kernel debugger symbols");
		dictionary.Add(524288L, "Disable paging of kernel stacks");
		dictionary.Add(1048576L, "Enable critical system breaks");
		dictionary.Add(2097152L, "Disable heap coalesce on free");
		dictionary.Add(4194304L, "Enable close exception");
		dictionary.Add(8388608L, "Enable exception logging");
		dictionary.Add(16777216L, "Enable object handle type tagging");
		dictionary.Add(33554432L, "Place heap allocations at ends of pages");
		dictionary.Add(67108864L, "Debug WINLOGON");
		dictionary.Add(134217728L, "Disable kernel-mode DbgPrint and KdPrint output");
		dictionary.Add(2147483648L, "Disable protected DLL verification");
		foreach (long key in dictionary.Keys)
		{
			int num = (int)key;
			if ((GFValue & num) == num)
			{
				text = text + ", " + dictionary[num];
			}
		}
		text = Globals.HelperFunctions.Right(text, Globals.HelperFunctions.Len(text) - 2);
		if (text == "")
		{
			text = "Unknown";
		}
		return text;
	}

	public bool IsPageHeapEnabled(int GFValue)
	{
		return (GFValue & 0x2000000) == 33554432;
	}
}
