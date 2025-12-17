using System;
using DebugDiag.DbgLib;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

public class VMFunctions
{
	private const int TopImageLimit = 10;

	private const int MEM_PRIVATE = 131072;

	private const int MEM_MAPPED = 262144;

	private const int MEM_IMAGE = 16777216;

	private const int MEM_COMMIT = 4096;

	private const int MEM_FREE = 65536;

	private const int MEM_RESERVE = 8192;

	private const int RegionUsageAll = 0;

	private const int RegionUsageIsVAD = 1;

	private const int RegionUsageFree = 2;

	private const int RegionUsageImage = 3;

	private const int RegionUsageThread = 4;

	private const int RegionUsageHeap = 5;

	private const int RegionUsagePageHeap = 6;

	private const int RegionUsageSystem = 7;

	private const int RegionUsageGCHeap = 8;

	private const int RegionUsageLOH = 9;

	private const int RegionUsageGCOther = 10;

	private static BarGraph VMGraph;

	public static void AnalyzeAndReportVMInfo()
	{
		int num = 0;
		ReportSection val = Globals.Manager.AddReportSection("VMReport", (SectionType)0);
		val.Title = "Virtual Memory Analysis";
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		Globals.HelperFunctions.ResetStatusNoIncrement("Loading virtual memory information. Please wait...");
		double filteredBlockSize = Globals.g_VMInfo.GetFilteredBlockSize(0, 0, 8192, 0.0);
		double filteredBlockSize2 = Globals.g_VMInfo.GetFilteredBlockSize(0, 0, 4096, 0.0);
		double filteredBlockSize3 = Globals.g_VMInfo.GetFilteredBlockSize(0, 0, 65536, 0.0);
		double largestFilteredBlockSize = Globals.g_VMInfo.GetLargestFilteredBlockSize(0, 0, 65536, 0.0);
		double largestFilteredBlockAddress = Globals.g_VMInfo.GetLargestFilteredBlockAddress(0, 0, 65536, 0.0);
		Globals.HelperFunctions.ResetStatusNoIncrement("Virtual memory information loaded.");
		double num2 = filteredBlockSize + filteredBlockSize2 + filteredBlockSize3;
		ReportSection val2 = val.AddChildSection("VMSummary", (SectionType)0);
		val2.Title = "Virtual Memory Summary";
		Globals.Manager.CurrentSection = val2;
		Globals.Manager.Write("\t<table class=myCustomText cellspacing=3>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Size of largest free VM block</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(largestFilteredBlockSize));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Free memory fragmentation</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Convert.ToString(Globals.HelperFunctions.FormatNumber((filteredBlockSize3 - largestFilteredBlockSize) / filteredBlockSize3 * 100.0, 2)));
		Globals.Manager.Write("%</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Free Memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize3));
		Globals.Manager.Write(" &nbsp;&nbsp;(");
		Globals.Manager.Write(Convert.ToString(Globals.HelperFunctions.FormatNumber(filteredBlockSize3 / num2 * 100.0, 2)));
		Globals.Manager.Write("% of Total Memory)</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Reserved Memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize));
		Globals.Manager.Write(" &nbsp;&nbsp;(");
		Globals.Manager.Write(Convert.ToString(Globals.HelperFunctions.FormatNumber(filteredBlockSize / num2 * 100.0, 2)));
		Globals.Manager.Write("% of Total Memory)</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Committed Memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize2));
		Globals.Manager.Write(" &nbsp;&nbsp;(");
		Globals.Manager.Write(Convert.ToString(Globals.HelperFunctions.FormatNumber(filteredBlockSize2 / num2 * 100.0, 2)));
		Globals.Manager.Write("% of Total Memory)</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Total Memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(num2));
		Globals.Manager.Write("</td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Largest free block at</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.g_Debugger.GetAs64BitHexString(largestFilteredBlockAddress));
		Globals.Manager.Write("</td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t</table>\r\n");
		Globals.Manager.Write("\t<br><br>\r\n");
		val2 = val.AddChildSection("VMDetails", (SectionType)0);
		val2.Title = "Virtual Memory Details";
		Globals.Manager.CurrentSection = val2;
		VMGraph = new BarGraph();
		VMGraph.SetRowCount(6);
		num = 0;
		double filteredBlockSize4 = Globals.g_VMInfo.GetFilteredBlockSize(1, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize4;
		VMGraph.Rows[num].Caption = "Virtual Allocations";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize4);
		VMGraph.Rows[num].Link = "#VADDetails" + Globals.g_UniqueReference;
		num++;
		double filteredBlockSize5 = Globals.g_VMInfo.GetFilteredBlockSize(3, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize5;
		VMGraph.Rows[num].Caption = "Loaded Modules";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize5);
		VMGraph.Rows[num].Link = "#ImageDetails" + Globals.g_UniqueReference;
		num++;
		double filteredBlockSize6 = Globals.g_VMInfo.GetFilteredBlockSize(4, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize6;
		VMGraph.Rows[num].Caption = "Threads";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize6);
		VMGraph.Rows[num].Link = "#ThreadDetails" + Globals.g_UniqueReference;
		num++;
		double filteredBlockSize7 = Globals.g_VMInfo.GetFilteredBlockSize(7, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize7;
		VMGraph.Rows[num].Caption = "System";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize7);
		num++;
		double filteredBlockSize8 = Globals.g_VMInfo.GetFilteredBlockSize(6, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize8;
		VMGraph.Rows[num].Caption = "Page Heaps";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize8);
		num++;
		double filteredBlockSize9 = Globals.g_VMInfo.GetFilteredBlockSize(5, 0, 0, 0.0);
		VMGraph.Rows[num].Value = filteredBlockSize9;
		VMGraph.Rows[num].Caption = "Native Heaps";
		VMGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(filteredBlockSize9);
		VMGraph.Rows[num].Link = "#HeapReport" + Globals.g_UniqueReference;
		VMGraph.DrawGraph();
		Globals.Manager.CurrentSection = val;
		PrintVMAllocReport();
		PrintLoadedModuleReport();
		PrintThreadReport();
		PrintSystemReport();
		PrintPageHeapReport();
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static void PrintVMAllocReport()
	{
		double filteredBlockSize = Globals.g_VMInfo.GetFilteredBlockSize(1, 0, 8192, 0.0);
		double filteredBlockSize2 = Globals.g_VMInfo.GetFilteredBlockSize(1, 0, 4096, 0.0);
		double filteredBlockSize3 = Globals.g_VMInfo.GetFilteredBlockSize(1, 262144, 0, 0.0);
		double num = Globals.g_VMInfo.GetFilteredBlockCount(1, 0, 8192, 0.0);
		double num2 = Globals.g_VMInfo.GetFilteredBlockCount(1, 0, 4096, 0.0);
		double num3 = Globals.g_VMInfo.GetFilteredBlockCount(1, 262144, 0, 0.0);
		ReportSection val = Globals.Manager.AddReportSection("VADDetails", (SectionType)0);
		val.Title = "Virtual Allocation Summary";
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("\t<table class=myCustomText>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Reserved memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Committed memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize2));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Mapped memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize3));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td>Reserved block count</td>\r\n");
		Globals.Manager.Write("\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(num.ToString());
		Globals.Manager.Write(" blocks</td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td>Committed block count</td>\r\n");
		Globals.Manager.Write("\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(num2.ToString());
		Globals.Manager.Write(" blocks</td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td>Mapped block count</td>\r\n");
		Globals.Manager.Write("\t\t\t<td>&nbsp;&nbsp;");
		Globals.Manager.Write(num3.ToString());
		Globals.Manager.Write(" blocks</td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t</table>\r\n");
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static void PrintThreadReport()
	{
		double filteredBlockSize = Globals.g_VMInfo.GetFilteredBlockSize(4, 0, 8192, 0.0);
		double filteredBlockSize2 = Globals.g_VMInfo.GetFilteredBlockSize(4, 0, 4096, 0.0);
		ReportSection val = Globals.Manager.AddReportSection("ThreadDetails", (SectionType)0);
		val.Title = "Thread Summary";
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("\t<table class=myCustomText ID=\"Table3\">\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Number of Threads</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.g_ThreadInfoCache.Count.ToString());
		Globals.Manager.Write(" Thread(s)</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Total reserved memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Total committed memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize2));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t</table>\r\n");
		Globals.Manager.Write("\t<br><br>\r\n");
		Globals.Manager.Write("\t<table class=myCustomText cellspacing=2 ID=\"Table4\">\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<th>Thread ID</th>\r\n");
		Globals.Manager.Write("\t\t\t<th>&nbsp;&nbsp;System ID</th>\r\n");
		Globals.Manager.Write("\t\t\t<th>&nbsp;&nbsp;EntryPoint</th>\r\n");
		Globals.Manager.Write("\t\t\t<th>&nbsp;&nbsp;Reserved</th>\r\n");
		Globals.Manager.Write("\t\t\t<th>&nbsp;&nbsp;Committed</th>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		for (int i = 0; i <= Convert.ToInt32(Globals.g_ThreadInfoCache.Count) - 1; i++)
		{
			filteredBlockSize = Globals.g_VMInfo.GetFilteredBlockSize(4, 0, 8192, Globals.g_ThreadInfoCache.Item(i).SystemID);
			filteredBlockSize2 = Globals.g_VMInfo.GetFilteredBlockSize(4, 0, 4096, Globals.g_ThreadInfoCache.Item(i).SystemID);
			Globals.Manager.Write("<tr>");
			Globals.Manager.Write("<td>" + Convert.ToString(Globals.g_ThreadInfoCache.Item(i).ThreadID) + "</td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;<b>" + Convert.ToString(Globals.g_ThreadInfoCache.Item(i).SystemID) + "</b></td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;<b>" + Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(Globals.g_UtilExt.GetEntryPointForThread((uint)Globals.g_ThreadInfoCache.Item(i).ThreadID))) + "</b></td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + Convert.ToString(Globals.HelperFunctions.PrintMemory(filteredBlockSize)) + "</td>");
			Globals.Manager.Write("<td nowrap>&nbsp;&nbsp;" + Convert.ToString(Globals.HelperFunctions.PrintMemory(filteredBlockSize2)) + "</td>");
			Globals.Manager.Write("</tr>");
		}
		Globals.Manager.Write("</table>");
		Globals.Manager.CurrentSection = val.Parent;
	}

	private static void PrintSystemReport()
	{
	}

	private static void PrintPageHeapReport()
	{
	}

	private static void PrintLoadedModuleReport()
	{
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Expected O, but got Unknown
		IModuleInfo val = null;
		BarGraph barGraph = null;
		IDbgModule[] array = null;
		val = Globals.g_Debugger.Modules;
		double filteredBlockSize = Globals.g_VMInfo.GetFilteredBlockSize(3, 0, 8192, 0.0);
		double filteredBlockSize2 = Globals.g_VMInfo.GetFilteredBlockSize(3, 0, 4096, 0.0);
		ReportSection val2 = Globals.Manager.AddReportSection("ImageDetails", (SectionType)0);
		val2.Title = "Loaded Module Summary";
		Globals.Manager.CurrentSection = val2;
		Globals.Manager.Write("\t<table class=myCustomText ID=\"Table1\">\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Number of Modules</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(val.Count.ToString());
		Globals.Manager.Write(" Modules</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Total reserved memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>Total committed memory</b></td>\r\n");
		Globals.Manager.Write("\t\t\t<td><b>&nbsp;&nbsp;");
		Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(filteredBlockSize2));
		Globals.Manager.Write("</b></td>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		Globals.Manager.Write("\t</table>\r\n");
		object[] array2 = (object[])val.GetModulesBySize();
		array = (IDbgModule[])(object)new IDbgModule[array2.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array[i] = (IDbgModule)array2[i];
		}
		if (Convert.ToBoolean(Globals.g_bShowTopImageGraph))
		{
			int num = ((val.Count <= 10) ? val.Count : 10);
			Globals.Manager.Write("<br><br><h2>Top " + Convert.ToString(num) + " modules by size</h2><br>");
			barGraph = new BarGraph();
			barGraph.SetRowCount(num);
			for (int j = 0; j <= num - 1; j++)
			{
				CacheFunctions.ScriptModuleClass scriptModuleClass = new CacheFunctions.ScriptModuleClass();
				scriptModuleClass.m_dbgModule = array[j];
				barGraph.Rows[j].Value = scriptModuleClass.Size;
				barGraph.Rows[j].Caption = "<b>" + scriptModuleClass.ModuleName + "</b>";
				barGraph.Rows[j].Caption2 = Globals.HelperFunctions.PrintMemory(scriptModuleClass.Size);
			}
			barGraph.DrawGraph();
		}
		Globals.Manager.Write("\t<br><br>\r\n");
		Globals.Manager.Write("\t<Font color='Red'><b>To view full details for any of the modules below, move your nouse over the specific module name</b></Font>\r\n");
		Globals.Manager.Write("\t<br><br>\r\n");
		Globals.Manager.Write("\t<table class=myCustomText cellspacing=2 ID=\"Table2\">\r\n");
		Globals.Manager.Write("\t\t<tr>\r\n");
		Globals.Manager.Write("\t\t\t<th>Module Name</th>\r\n");
		Globals.Manager.Write("\t\t\t<th>&nbsp;&nbsp;Size</th>\r\n");
		Globals.Manager.Write("\t\t</tr>\r\n");
		for (int j = 0; j < array.Length; j++)
		{
			CacheFunctions.ScriptModuleClass scriptModuleClass2 = new CacheFunctions.ScriptModuleClass();
			scriptModuleClass2.m_dbgModule = array[j];
			Globals.Manager.Write("\t\t<tr>\r\n");
			Globals.Manager.Write("\t\t\t<td>\r\n");
			Globals.Manager.Write(string.Format("<div onmouseover=\"ShowContent('{0}'); return true;\" onmouseout=\"HideContent('{0}'); return true;\" onclick=\"ShowContent('{0}'); return true;\">{0}</div>", scriptModuleClass2.ModuleName));
			string arg = $"<b>Company:</b> {scriptModuleClass2.VSCompanyName}<br><b>Product:</b> {scriptModuleClass2.VSProductName}<br><b>File Description:</b> {scriptModuleClass2.VSFileDescription}<br><b>Version:</b> {scriptModuleClass2.VSFileVersion}<br><b>TimeStamp:</b> {scriptModuleClass2.TimeStamp}<br> <b>Base Address:</b> {Globals.HelperFunctions.GetAsHexString(scriptModuleClass2.Base)}";
			Globals.Manager.Write($"<div id=\"{scriptModuleClass2.ModuleName}\" style=\"display:none; position:absolute; border-style: solid; background-color: white; padding: 5px;\">{arg}</div>");
			Globals.Manager.Write("\t\t\t</td>\r\n");
			Globals.Manager.Write("\t\t\t<td nowrap>&nbsp;&nbsp;<b>");
			Globals.Manager.Write(Globals.HelperFunctions.PrintMemory(scriptModuleClass2.Size));
			Globals.Manager.Write("</b></td>\r\n");
			Globals.Manager.Write("\t\t</tr>\r\n");
		}
		Globals.Manager.Write("</table>");
		Globals.Manager.CurrentSection = val2.Parent;
	}
}
