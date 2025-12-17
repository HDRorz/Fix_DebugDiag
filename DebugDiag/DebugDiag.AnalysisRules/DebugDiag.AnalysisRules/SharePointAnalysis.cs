using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.AnalysisRules;

public class SharePointAnalysis : IAnalysisRuleMetadata, IMultiDumpRule, IAnalysisRuleBase, IMultiDumpRuleFilter
{
	private AnalysisModes _analysisMode;

	public string NewLineStr => "<br />";

	public string Category => "Product-Specific Analyzers";

	public string Description => "Crash and hang analysis with specific reporting for SharePoint";

	public string Tag(string Tag, string InnerHtml)
	{
		return string.Format("<{0}>{1}</{0}>", Tag, InnerHtml);
	}

	public string Bold(string HtmlText)
	{
		return Tag("b", HtmlText);
	}

	public string Header1(string HtmlText)
	{
		return Tag("h1", HtmlText);
	}

	public string Header2(string HtmlText)
	{
		return Tag("h2", HtmlText);
	}

	public string Header3(string HtmlText)
	{
		return Tag("h3", HtmlText);
	}

	public string NewLine(int Lines)
	{
		StringBuilder stringBuilder = new StringBuilder(Lines * NewLineStr.GetSafeLength());
		for (int i = 0; i < Lines; i++)
		{
			stringBuilder.Append(NewLineStr);
		}
		return stringBuilder.ToString();
	}

	public string GetLineStartingWith(string Text, string Start, bool Trim = true)
	{
		int num = Text.IndexOf(Start);
		if (num < 0)
		{
			return null;
		}
		num += Start.GetSafeLength();
		int num2 = Text.IndexOf("\n", num);
		if (num2 - num <= 0)
		{
			return null;
		}
		string text = Text.Substring(num, num2 - num);
		if (Trim)
		{
			return text.Trim();
		}
		return text;
	}

	public bool IsWSSProcess(NetDbgObj Debugger)
	{
		if (Debugger.Execute("lmv mSTSWEL").Contains("STSWEL.DLL"))
		{
			return true;
		}
		return false;
	}

	public string GetDllField(string Text, DllField Field)
	{
		return Field switch
		{
			DllField.CheckSum => GetLineStartingWith(Text, "CheckSum:"), 
			DllField.Company => GetLineStartingWith(Text, "CompanyName:"), 
			DllField.Copyright => GetLineStartingWith(Text, "LegalCopyright:"), 
			DllField.Description => GetLineStartingWith(Text, "FileDescription:"), 
			DllField.FileName => GetLineStartingWith(Text, "Image name:"), 
			DllField.FileVersion => GetLineStartingWith(Text, "FileVersion:"), 
			DllField.ImageSize => GetLineStartingWith(Text, "ImageSize:"), 
			DllField.Product => GetLineStartingWith(Text, "ProductName:"), 
			DllField.TimeStamp => GetLineStartingWith(Text, "Timestamp"), 
			_ => "", 
		};
	}

	public string GetDllDetails(NetDbgObj Debugger, string Dll)
	{
		string text = Debugger.Execute($"lmv m{Dll.Replace('.', ' ')}");
		if (text.GetSafeLength() < 100)
		{
			return null;
		}
		return text;
	}

	public string GetUniqueReference(NetDbgObj Debugger)
	{
		return $"{Debugger.ProcessID}_{Debugger.ProcessUpTime}_{Debugger.DumpFileFullPath.GetHashCode()}";
	}

	public string AsList(IEnumerable<string> StrList, string Separator = ",")
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string Str in StrList)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(Separator);
			}
			stringBuilder.AppendFormat(" {0}", Str);
		}
		return stringBuilder.ToString();
	}

	private static object GetFieldValue(ClrHeap heap, ulong Address, ClrType TheType, string FieldName)
	{
		string[] array = FieldName.Split('.');
		ulong num = Address;
		ClrType val = TheType;
		ClrInstanceField fieldByName;
		for (int i = 0; i < array.Length - 1; i++)
		{
			fieldByName = val.GetFieldByName(array[i]);
			if (!((ClrField)fieldByName).IsObjectReference)
			{
				return null;
			}
			num = (ulong)fieldByName.GetValue(num);
			if (num == 0L)
			{
				return null;
			}
			val = heap.GetObjectType(num);
		}
		fieldByName = val.GetFieldByName(array[array.Length - 1]);
		return fieldByName.GetValue(num);
	}

	private static string GetFieldValueString(ulong Address, ClrType TheType, string FieldName)
	{
		ClrInstanceField fieldByName = TheType.GetFieldByName(FieldName);
		if (fieldByName == null)
		{
			return string.Format("{invalid field: 0} at {1:x}", FieldName, Address);
		}
		return GetOutput(Address, fieldByName);
	}

	private static string GetOutput(ulong obj, ClrInstanceField field)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected I4, but got Unknown
		if (!((ClrField)field).HasSimpleValue)
		{
			return field.GetAddress(obj).ToString("X");
		}
		object fieldValue = field.GetValue(obj);
		if (fieldValue == null)
		{
			return "{error}";
		}
		ClrElementType elementType = ((ClrField)field).ElementType;
		if ((int)elementType != 14)
		{
			switch ((ClrElementType)((int)elementType - 18))
			{
			case ClrElementType.Unknown:
			case ClrElementType.Boolean:
			case ClrElementType.Int16:
			case ClrElementType.UInt16:
			case ClrElementType.UInt32:
			case ClrElementType.Int64:
			case ClrElementType.UInt64:
				return $"{fieldValue:X}";
			default:
				return fieldValue.ToString();
			}
		}
		return (string)fieldValue;
	}

	public string RightMost(string Str, int Length)
	{
		return Str?.Substring(Math.Max(Str.Length - Length, 0));
	}

	public string TypeColor(string TypeName)
	{
		return $"<span style='color:Teal;'>{TypeName}</span>";
	}

	public void RunAnalysisRule(NetScriptManager Manager, NetProgress Progress)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		Globals.Manager = Manager;
		Globals.g_Progress = Progress;
		Globals.g_Progress.SetOverallRange(0, Globals.Manager.GetDumpFiles().Count * 7);
		Globals.HelperFunctions.LoadProcessesInThisReport();
		Globals.HelperFunctions.ResetStatusNoIncrement("Building the table of contents");
		Manager.Write("<h4>Table Of Contents</h4>");
		Progress.OverallStatus = "Building the Table of contents";
		foreach (string dumpFile in Globals.Manager.GetDumpFiles())
		{
			if (!Path.GetExtension(dumpFile).ToLower().Contains("dmp"))
			{
				continue;
			}
			NetDbgObj debugger = Globals.Manager.GetDebugger(dumpFile);
			try
			{
				if (debugger.IsKernelMode)
				{
					Manager.WriteLine($"The SharePointAnalysis rule applies only to <b>usermode</b> dumps.  <font color='red'>Skipping analysis for <b>kernel</b> dump file <b>{Globals.g_ShortDumpFileName}</b></font>");
					continue;
				}
				string filterReason = null;
				if (debugger == null)
				{
					continue;
				}
				if (ShouldRunAnalysis(debugger, _analysisMode, ref filterReason))
				{
					string text = $"<a href='#Dump{GetUniqueReference(debugger)}-t'><h2>Analysis of {Path.GetFileName(dumpFile)}</h2></a><br />";
					Manager.WriteLine(text);
					string[] array = new string[7] { "List of custom SharePoint types", "List of Safe Controls in Web.Config", "SPWeb Objects list", "SPSite Object Lists", "SPRequest statistics", "Threads with similar SPRequest allocation stack", "Thread Summary" };
					foreach (string text2 in array)
					{
						Manager.WriteLine($"<li><a href='#Dump{GetUniqueReference(debugger)}-{text2.GetHashCode()}'>{text2}</a></li>");
					}
				}
				else
				{
					Manager.WriteLine($"<h2>Analysis of {Path.GetFileName(dumpFile)}&nbsp;&nbsp;&nbsp;<i>(Skipped: {filterReason})</i><br><br></h2><br />");
				}
			}
			finally
			{
				((IDisposable)debugger)?.Dispose();
			}
		}
		foreach (string dumpFile2 in Globals.Manager.GetDumpFiles())
		{
			Globals.g_DataFile = dumpFile2;
			Globals.g_Debugger = null;
			NetDbgObj val = (Globals.g_Debugger = Globals.Manager.GetDebugger(Globals.g_DataFile));
			try
			{
				string filterReason2 = null;
				if (!ShouldRunAnalysis(Globals.g_Debugger, _analysisMode, ref filterReason2))
				{
					Manager.WriteLine($"{filterReason2}.  Skipping analysis for dump file <b>{Globals.g_ShortDumpFileName}</b>");
					continue;
				}
				NetDbgObj g_Debugger = Globals.g_Debugger;
				string dumpFileShortName = g_Debugger.DumpFileShortName;
				if (Path.GetExtension(dumpFileShortName).ToLower().Contains("dmp") && g_Debugger != null)
				{
					Manager.WriteLine($"<a name='#Dump{GetUniqueReference(g_Debugger)}-t'><br /></a>");
				}
				if (!Path.GetExtension(dumpFileShortName).ToLower().Contains("dmp"))
				{
					continue;
				}
				Globals.HelperFunctions.SetOSVersion();
				Globals.g_ShortDumpFileName = dumpFileShortName;
				if (g_Debugger != null)
				{
					string text3 = $"<a href='#Dump{GetUniqueReference(g_Debugger)}-t'><b>{Path.GetFileName(dumpFileShortName)}</b></a><br />";
					string uniqueReference = GetUniqueReference(g_Debugger);
					Globals.g_UniqueReference = GetUniqueReference(g_Debugger);
					string fileName = Path.GetFileName(dumpFileShortName);
					Globals.HelperFunctions.StartToggleSection($"Report for {Globals.g_ShortDumpFileName}", $"SharePointAnalysis:{Globals.g_UniqueReference}", startCollapsed: false);
					Manager.Write($"<h1>Report for <a name='Dump{uniqueReference}-t'>{fileName}</a></h1>");
					g_Debugger.Execute("|");
					Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, "SharePoint Analysis");
					Progress.CurrentStatus = $"SharePoint Analysis on {dumpFileShortName}";
					DateTime.Now.ToString();
					string dllDetails = GetDllDetails(g_Debugger, "STSWEL");
					string dllDetails2 = GetDllDetails(g_Debugger, "OWSSVR");
					string text4 = null;
					if (!string.IsNullOrEmpty(dllDetails2))
					{
						text4 = GetDllField(dllDetails2, DllField.FileVersion);
						if (g_Debugger.ClrRuntime != null)
						{
							Manager.ReportInformation($"SharePoint Server version {text4} was found in dump file: {text3}", 0, "{0281F079-EAAC-4458-87FF-84143EB50F3B}");
						}
					}
					else if (!string.IsNullOrEmpty(dllDetails))
					{
						text4 = GetDllField(dllDetails, DllField.FileVersion);
						if (g_Debugger.ClrRuntime != null)
						{
							Manager.ReportInformation($"SharePoint Services/Foundation version {text4} was found in dump file: {text3}", 0, "{31D6735D-325B-4E40-95ED-46BBF362330F}");
						}
					}
					if (text4 == null)
					{
						Manager.ReportOther($"SharePoint was not detected in the dump file: <br /> {text3}", "SharePoint analysis will be skipped", "Notification", "notificationicon.png", 0, "");
						continue;
					}
					if (g_Debugger.ClrRuntime == null)
					{
						Manager.ReportError($"SharePoint version {text4} was found, but the DAC Library to analyze the following dump file could not be loaded:<br />{text3}.", "SharePoint analysis will be <b>forcibly</b> skipped.<br />Locate the correct DAC file and try again.", 10, "{AAF5A24D-9A61-45C3-B464-0063693A3589}");
						continue;
					}
					ClrHeap heap = g_Debugger.ClrRuntime.Heap;
					HeapCache heapCache = new HeapCache(g_Debugger.ClrRuntime);
					string text5 = "List of custom SharePoint types";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					Manager.WriteLine("<code>");
					Manager.WriteLine(HttpUtility.HtmlEncode(string.Format("{0}\t{1,10}\t{2,10}\t{3,10}\t{4,10}\t{5,10}\t{6,10}", "Type Name", "Count", "Total Size", "Average", "Std Dev", "Minimum", "Maximum")).Replace(" ", "&nbsp;"));
					ulong num = 0uL;
					ulong num2 = 0uL;
					SortedDictionary<string, HeapStatItem> sortedDictionary = new SortedDictionary<string, HeapStatItem>();
					foreach (HeapStatItem item in heapCache.EnumerateTypesStats())
					{
						if ((item.IsDerivedOrImplementationOf("Microsoft.SharePoint.*") || item.IsDerivedOrImplementationOf("Microsoft.IdentityModel.*") || item.IsDerivedOrImplementationOf("Microsoft.Office.Server.*") || item.IsDerivedOrImplementationOf("System.Web.UI.WebControls.WebParts.*")) && !item.Name.StartsWith("Microsoft.") && !item.Name.StartsWith("System."))
						{
							sortedDictionary.Add(item.Name, item);
						}
					}
					foreach (HeapStatItem value in sortedDictionary.Values)
					{
						if (value.Count > 1 || value.TotalSize >= 1)
						{
							Manager.WriteLine(string.Format("<b>{0}</b>: {1} {2}<br />{3}", TypeColor(HttpUtility.HtmlEncode(value.Name)), TypeColor(HttpUtility.HtmlEncode(AsList(value.Interfaces))), TypeColor(HttpUtility.HtmlEncode(AsList(value.InheritanceChain))), HttpUtility.HtmlEncode($"\t\t\t{value.Count,20:n0}\t{value.TotalSize,10:n0}\t{value.Average,10:n0}\t{value.StdDeviation,10:n0}\t{value.MinSize,10:n0}\t{value.MaxSize,10:n0}").Replace(" ", "&nbsp;")));
						}
						num += value.Count;
						num2 += value.TotalSize;
					}
					Manager.WriteLine();
					Manager.WriteLine($"Total: {num:n0} Sharepoint Pages and Custom SharePoint Objects. {num2:n0} Bytes");
					Manager.WriteLine("</code>");
					Globals.HelperFunctions.UpdateOverallProgress();
					text5 = "List of Safe Controls in Web.Config";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Globals.HelperFunctions.EndToggleSection();
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					int num3 = 0;
					int num4 = 0;
					var enumerable = (from o in heapCache.EnumerateObjectsOfType("Microsoft.SharePoint.ApplicationRuntime.SafeControlConfigTypeEntry")
						let t = heap.GetObjectType(o)
						orderby GetFieldValueString(o, t, "_namespace"), GetFieldValueString(o, t, "_typeName"), GetFieldValueString(o, t, "_assemblyNormalizedName")
						select new
						{
							Assembly = GetFieldValueString(o, t, "_assemblyNormalizedName"),
							Namespace = GetFieldValueString(o, t, "_namespace"),
							TypeName = GetFieldValueString(o, t, "_typeName"),
							IsSafe = GetFieldValueString(o, t, "_isSafe"),
							AllowRemoteDesigner = GetFieldValueString(o, t, "_allowRemoteDesigner")
						}).Distinct();
					Manager.WriteLine("<code>");
					Manager.WriteLine(HttpUtility.HtmlEncode(string.Format("{0:-50}\t{1,-8}\t{2,-15}\t{3}", "Type", "Safe", "Remote Design", "Assembly")).Replace(" ", "&nbsp;"));
					foreach (var item2 in enumerable)
					{
						num4++;
						if (item2.AllowRemoteDesigner == "True")
						{
							num3++;
						}
						Manager.WriteLine($"<b>{TypeColor(HttpUtility.HtmlEncode(item2.Namespace))}.{TypeColor(HttpUtility.HtmlEncode(item2.TypeName))}</b>\t{item2.IsSafe:-8}\t{item2.AllowRemoteDesigner,-15}\t{item2.Assembly}");
					}
					Manager.WriteLine();
					Manager.WriteLine($"{num4:n0} Safe Types Entries found in memory. {num3:n0} Safe for remote designer.");
					Manager.WriteLine("</code>");
					Globals.HelperFunctions.UpdateOverallProgress();
					SortedDictionary<string, List<SPRequestObj>> sortedDictionary2 = new SortedDictionary<string, List<SPRequestObj>>();
					Dictionary<ulong, List<SPRequestObj>> dictionary = new Dictionary<ulong, List<SPRequestObj>>();
					text5 = "SPWeb Objects list";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Globals.HelperFunctions.EndToggleSection();
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					Manager.WriteLine("<code>");
					Manager.WriteLine("Address          SPRequest Auto-Dispose Auth Thread Url [Title]".Replace(" ", "&nbsp;"));
					int num5 = 0;
					int num6 = 0;
					int num7 = 0;
					int num8 = 0;
					foreach (ulong item3 in heapCache.EnumerateObjectsOfType("Microsoft.SharePoint.SPWeb"))
					{
						Dictionary<string, object> fields = heapCache.GetFields(item3, "m_Request,m_Request.m_SPRequest, m_Request.m_AllowCleanupWhenThreadEnds, m_bInited, m_Url, m_RequestOwnedByThisWeb, m_CurrentUser.m_isWindowsAccount, m_Request.m_UnmanagedThreadId, m_Request.m_ManagedThreadId, m_strTitle,m_CurrentUser");
						num7++;
						bool flag = (bool)fields["m_bInited"];
						bool flag2 = (ulong)fields["m_Request"] == 0;
						if (flag)
						{
							num6++;
						}
						if (flag2)
						{
							num5++;
							continue;
						}
						int num9 = (int)fields["m_Request.m_UnmanagedThreadId"];
						string text6 = "****";
						NetDbgThread threadBySystemID = g_Debugger.GetThreadBySystemID(num9);
						text6 = $"{threadBySystemID.ThreadID,4}";
						if (!sortedDictionary2.ContainsKey(text6))
						{
							sortedDictionary2[text6] = new List<SPRequestObj>();
						}
						sortedDictionary2[text6].Add(new SPRequestObj(SPRequestType.SPWeb, item3, (ulong)fields["m_Request"], num9));
						ulong key = (ulong)fields["m_Request"];
						if (!dictionary.ContainsKey(key))
						{
							dictionary[key] = new List<SPRequestObj>();
						}
						dictionary[key].Add(new SPRequestObj(SPRequestType.SPWeb, item3, (ulong)fields["m_Request"], num9));
						if (!(bool)fields["m_Request.m_AllowCleanupWhenThreadEnds"])
						{
							num8++;
						}
						string text7 = (string)fields["m_strTitle"];
						Manager.WriteLine(string.Format("{0:x016} {1} {2} {3} {4} {5} [{6}]", item3, ((bool)fields["m_RequestOwnedByThisWeb"]) ? "Own      " : "Not Owned", ((bool)fields["m_Request.m_AllowCleanupWhenThreadEnds"]) ? "Thread     " : "False      ", ((ulong)fields["m_CurrentUser"] == 0L) ? "(null) " : (((bool)fields["m_CurrentUser.m_isWindowsAccount"]) ? "Windows" : "Claims "), string.Format("<a href='#Dump{0}_t{1}'>{2}</a> ", GetUniqueReference(g_Debugger), text6.GetHashCode(), text6.Replace(" ", "&nbsp;")).Replace(" ", "\t"), HttpUtility.HtmlEncode((fields["m_Url"] == null) ? "(null)" : ((string)fields["m_Url"])), HttpUtility.HtmlEncode((string.IsNullOrEmpty(text7) || text7.Length < 30) ? text7 : text7.Substring(0, 30))).Replace(" ", "&nbsp;"));
					}
					Manager.WriteLine($"<br />{num7:n0} SPWeb objects found. {num7 - num5:n0} Undisposed. {num8:n0} Potentialy leaked. {num7 - num6:n0} not fully initiated.");
					Manager.WriteLine("</code>");
					Globals.HelperFunctions.UpdateOverallProgress();
					text5 = "SPSite Object Lists";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Globals.HelperFunctions.EndToggleSection();
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					Manager.WriteLine("<code>");
					Manager.WriteLine("Address          Thread Anon Impers Zone     Open Webs Relative Url (User Name) [Called Via]".Replace(" ", "&nbsp;"));
					bool flag3 = text4.StartsWith("12.");
					string text8 = (flag3 ? "m_openedWebs._size" : "m_openedWebs.m_HandleList._size");
					string fields2 = $"m_Zone, m_UserName, m_strServerRelativeUrl, m_OriginalUri, m_OriginalUri.m_String, m_bAsAnonymous, m_bImpersonating, m_bInited, m_Request,m_Request.m_SPRequest, m_Request.m_AllowCleanupWhenThreadEnds, m_Request.m_UnmanagedThreadId, m_Request.m_ManagedThreadId, {text8}";
					int num10 = 0;
					int num11 = 0;
					int num12 = 0;
					int num13 = 0;
					foreach (ulong item4 in heapCache.EnumerateObjectsOfType("Microsoft.SharePoint.SPSite"))
					{
						Dictionary<string, object> fields3 = heapCache.GetFields(item4, fields2);
						num10++;
						bool flag4 = (bool)fields3["m_bInited"];
						bool flag5 = (ulong)fields3["m_Request"] == 0;
						if (flag4)
						{
							num12++;
						}
						if (flag5)
						{
							num11++;
							continue;
						}
						int num14 = (int)fields3["m_Request.m_UnmanagedThreadId"];
						string text9 = "****";
						NetDbgThread threadBySystemID2 = g_Debugger.GetThreadBySystemID(num14);
						text9 = $"{threadBySystemID2.ThreadID,4}";
						if (!sortedDictionary2.ContainsKey(text9))
						{
							sortedDictionary2[text9] = new List<SPRequestObj>();
						}
						sortedDictionary2[text9].Add(new SPRequestObj(SPRequestType.SPSite, item4, (ulong)fields3["m_Request"], num14));
						ulong key2 = (ulong)fields3["m_Request"];
						if (!dictionary.ContainsKey(key2))
						{
							dictionary[key2] = new List<SPRequestObj>();
						}
						dictionary[key2].Add(new SPRequestObj(SPRequestType.SPSite, item4, (ulong)fields3["m_Request"], num14));
						if (!(bool)fields3["m_Request.m_AllowCleanupWhenThreadEnds"])
						{
							num13++;
						}
						Manager.WriteLine(string.Format("{0:x016}   {1} {2} {3} {4,-8} {5,9} {6} ({7}) [{8}]", item4, string.Format("<a href='#Dump{0}_t{1}'>{2}</a> ", GetUniqueReference(g_Debugger), text9.GetHashCode(), text9.Replace(" ", "&nbsp;")).Replace(" ", "\t"), ((bool)fields3["m_bAsAnonymous"]) ? "Yes " : "No  ", ((bool)fields3["m_bImpersonating"]) ? "Yes   " : "No    ", (string)fields3["m_Zone"], (fields3[text8] != null) ? ((int)fields3[text8]) : 0, (string)fields3["m_strServerRelativeUrl"], HttpUtility.HtmlEncode((fields3["m_UserName"] == null) ? "N.A." : ((string)fields3["m_UserName"])), HttpUtility.HtmlEncode(((ulong)fields3["m_OriginalUri"] == 0L || fields3["m_OriginalUri.m_String"] == null) ? "(null)" : ((string)fields3["m_OriginalUri.m_String"]))).Replace(" ", "&nbsp;"));
					}
					Manager.WriteLine($"<br />{num10:n0} SPSite objects found. {num10 - num11:n0} Undisposed. {num13:n0} Pontentially leaked. {num10 - num12:n0} not fully initiated.");
					Manager.WriteLine("</code>");
					Globals.HelperFunctions.UpdateOverallProgress();
					text5 = "SPRequest statistics";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Globals.HelperFunctions.EndToggleSection();
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					int num15 = 0;
					int num16 = 0;
					int num17 = 0;
					int num18 = 0;
					bool flag6 = true;
					Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
					Manager.WriteLine("<code>");
					Manager.WriteLine("SPContext        Thread Linked Objects".Replace(" ", "&nbsp;"));
					foreach (ulong item5 in heapCache.EnumerateObjectsOfType("Microsoft.SharePoint.Library.SPRequest"))
					{
						Dictionary<string, object> fields4 = heapCache.GetFields(item5, flag3 ? "m_SPRequest, m_AllowCleanupWhenThreadEnds, m_UnmanagedThreadId, m_ManagedThreadId,m_StackTrace" : "m_SPRequest, m_AllowCleanupWhenThreadEnds, m_UnmanagedThreadId, m_Name, m_Disposed,m_StackTrace");
						num15++;
						if (flag3 ? ((ulong)fields4["m_SPRequest"] == 0) : ((bool)fields4["m_Disposed"]))
						{
							num17++;
							continue;
						}
						int num19 = (int)fields4["m_UnmanagedThreadId"];
						string text10 = "****";
						NetDbgThread threadBySystemID3 = g_Debugger.GetThreadBySystemID(num19);
						text10 = $"{threadBySystemID3.ThreadID,4}";
						string text11 = (string)fields4["m_StackTrace"];
						if (!string.IsNullOrWhiteSpace(text11))
						{
							flag6 = false;
							string key3 = TypeColor(HttpUtility.HtmlEncode(text11)).Replace("\r", "<br />");
							if (!dictionary2.ContainsKey(key3))
							{
								dictionary2[key3] = new List<string>();
							}
							dictionary2[key3].Add(text10);
						}
						if (!(bool)fields4["m_AllowCleanupWhenThreadEnds"])
						{
							num18++;
						}
						if (!sortedDictionary2.ContainsKey(text10))
						{
							sortedDictionary2[text10] = new List<SPRequestObj>();
						}
						sortedDictionary2[text10].Add(new SPRequestObj(SPRequestType.SPRequest, item5, item5, num19));
						int num20 = 0;
						int num21 = 0;
						StringBuilder stringBuilder = new StringBuilder();
						if (dictionary.ContainsKey(item5))
						{
							foreach (SPRequestObj item6 in dictionary[item5])
							{
								switch (item6.Type)
								{
								case SPRequestType.SPSite:
									num21++;
									stringBuilder.AppendFormat("SPSite:{0:x16} ", item6.Address);
									break;
								case SPRequestType.SPWeb:
									stringBuilder.AppendFormat("SPSWeb:{0:x16} ", item6.Address);
									num20++;
									break;
								}
							}
						}
						if (stringBuilder.Length == 0)
						{
							stringBuilder.Append("Orphaned");
						}
						Manager.WriteLine(string.Format("{0:x016}   {1}{2}", item5, string.Format("<a href='#Dump{0}_t{1}'>{2}</a> ", GetUniqueReference(g_Debugger), text10.GetHashCode(), text10.Replace(" ", "&nbsp;")).Replace(" ", "\t"), stringBuilder.ToString()).Replace(" ", "&nbsp;"));
						if (num21 + num20 == 0)
						{
							num16++;
							dictionary[item5] = new List<SPRequestObj>();
						}
						dictionary[item5].Add(new SPRequestObj(SPRequestType.SPRequest, item5, item5, num19));
					}
					if (flag6 && num15 > num17)
					{
						Manager.ReportWarning($"SharePoint is not configured to trace stack trace during SPRequest Allocation and the Stack Allocation Analysis was not performed for dump.<br />{text3}<br />This is not an error. By default, SharePoint does not log the allocation stack as it adds time overhead to log the stack.<br />If you find many undisposed SPRequest objects you may want to enable logging to narrow down the problem", flag3 ? "To enable stack trace on SPRequest allocation, set thus registry entry in all SharePoint servers: HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Shared Tools\\Web Server Extensions\\HeapSettings\\SPRequestStackTrace = 1" : "To enable stack trace on SPRequest allocation, run the PowerShell Command:<br /><code>[System.Reflection.Assembly]::LoadWithPartialName(\"Microsoft.SharePoint\")<br />$cs = [Microsoft.SharePoint.Administration.SPWebService]::ContentService<br />$cs.CollectSPRequestAllocationCallStacks = $true<br />$cs.Update()<br />", 10, "{D18DEAE5-6652-40D4-BEAC-48FA3E563C1C}");
						flag6 = true;
					}
					if (num15 - num17 > 0)
					{
						Manager.ReportWarning($"{num15 - num17} SPRequest Objects were found undisposed in dump file: {text3}", "Some SPRequest allocations are made to be automatically disposed when the thread exits. If no SPRequest is leaked you may ignore this warning", 10, "{1F2A3FDD-B857-4FCC-9F07-8245FAEFC986}");
					}
					if (num18 > 0)
					{
						Manager.ReportError($"{num18} SPRequest Objects were not disposed of properly and will outlive the thread where they were created causing memory leak in dump file: <br /> {text3}", "Please verify the stack trace leading to this leak and make the appropriate adjustments", 1, "{85BBA0C9-00F1-45D5-916B-5D05D6D3E3A4}");
					}
					Manager.WriteLine($"<br />{num15:n0} SPRequest objects found. {num15 - num17:n0} Undisposed. {num18:n0} Leaked SPRequests. {num16:n0} not linked to a SPWeb or SPSite object.");
					Manager.WriteLine("</code>");
					Globals.HelperFunctions.UpdateOverallProgress();
					Globals.HelperFunctions.EndToggleSection();
					text5 = "Threads with similar SPRequest allocation stack";
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					if (dictionary2.Count > 0)
					{
						Manager.WriteLine("<code>");
						foreach (string key5 in dictionary2.Keys)
						{
							Manager.WriteLine("<hr />");
							Manager.Write("Thread Id(s): ");
							foreach (string item7 in dictionary2[key5].Distinct())
							{
								Manager.Write($"<a href='#Dump{GetUniqueReference(g_Debugger)}_t{item7.GetHashCode()}'>{item7}</a> ");
							}
							Manager.WriteLine("<br />Stack During Allocation: <br />");
							Manager.WriteLine(key5);
							Manager.WriteLine("<br />");
						}
						Manager.WriteLine("</code>");
					}
					else
					{
						Manager.WriteLine("No stack trace allocation record found");
					}
					Globals.HelperFunctions.UpdateOverallProgress();
					text5 = "Thread Summary";
					Globals.HelperFunctions.ResetStatusNoIncrement(text5);
					Globals.HelperFunctions.EndToggleSection();
					Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}-{text5.GetHashCode()}'>{Header2(text5)}</a>");
					Globals.HelperFunctions.StartToggleSection("", $"{GetUniqueReference(g_Debugger)}{text5.GetHashCode()}", startCollapsed: false);
					List<NetDbgThread> managedThreads = g_Debugger.ManagedThreads;
					foreach (string key4 in sortedDictionary2.Keys)
					{
						if (key4 == "***")
						{
							continue;
						}
						Manager.Write($"<a name='#Dump{GetUniqueReference(g_Debugger)}_t{key4.GetHashCode()}'> </a>");
						Manager.Write($"Thread {key4}");
						Manager.Write("<hr />");
						Manager.WriteLine("<code>");
						Manager.Write("Linked Objects: ");
						foreach (SPRequestObj item8 in sortedDictionary2[key4].Distinct())
						{
							Manager.Write($"{item8.Type} ({item8.Address:x16}) ");
						}
						NetDbgThread val2 = managedThreads.Find((NetDbgThread t) => t.ThreadID == int.Parse(key4));
						if (val2 != null)
						{
							if (val2.WaitingOnCritSecAddr != 0.0)
							{
								Manager.Write($"<br />Waiting on Critical Object {(ulong)val2.WaitingOnCritSecAddr:x16}");
							}
							Manager.WriteLine("<br /><b>Stack:</b><br />");
							foreach (NetDbgStackFrame item9 in (List<NetDbgStackFrame>)(object)val2.ManagedStackFrames)
							{
								string frameText = item9.GetFrameText(true, false);
								if (!frameText.Trim().StartsWith("["))
								{
									string[] array2 = frameText.Split('!');
									string text12 = $"{TypeColor(HttpUtility.HtmlEncode(array2[array2.Length - 1]))}<br />";
									Manager.Write(text12);
								}
							}
						}
						Manager.WriteLine("</code>");
					}
					Globals.HelperFunctions.EndToggleSection();
					Globals.HelperFunctions.UpdateOverallProgress();
					goto IL_1aa5;
				}
				Manager.ReportError("Unable to open the file " + dumpFileShortName + " for analysis.", "The file may be corrupt, in which case a new dump file of the targeted process will have to be created to do any analysis.", 0, "{cbdb676b-5984-4889-ad61-639cb79d78d7}");
				goto IL_1aa5;
				IL_1aa5:
				UpdateOverallProgress();
				Globals.HelperFunctions.EndToggleSection();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public void RunCheckForCategory(string checkListsToRun, string stringGUID)
	{
	}

	public void UpdateOverallProgress()
	{
	}

	public bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
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
		filterReason = "None of the selected dumps are full dumps with the SharePoint runtime loaded.";
		return false;
	}

	private bool ShouldRunAnalysis(NetDbgObj debugger, AnalysisModes mode, ref string filterReason)
	{
		if (debugger.DumpType == "MINIDUMP")
		{
			filterReason = debugger.DumpFileShortName + " is a <b>mini</b> dump.  SharePointAnalysis requires a <b>full</b> usermode dump with the SharePoint Runtime loaded.";
			return false;
		}
		if (debugger.IsKernelMode)
		{
			filterReason = debugger.DumpFileShortName + " is a <b>kernel</b> dump.  SharePointAnalysis requires a full <b>usermode</b> dump with the SharePoint Runtime loaded.";
			return false;
		}
		if (debugger.GetModuleByModuleName("STSWEL") != null)
		{
			return true;
		}
		if (debugger.GetModuleByModuleName("OWSSVR") != null)
		{
			return true;
		}
		filterReason = debugger.DumpFileShortName + " does not have the SharePoint Runtime loaded. SharePointAnalysis requires a full usermode dump <b>with the SharePoint Runtime loaded.</b>";
		return false;
	}
}
