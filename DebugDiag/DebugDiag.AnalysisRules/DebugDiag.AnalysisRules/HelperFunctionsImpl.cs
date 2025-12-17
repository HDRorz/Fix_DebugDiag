using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CrashHangExtLib;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.HtmlHelpers;
using DebugDiag.DotNet.Reports;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

internal class HelperFunctionsImpl : IHelperFunctions
{
	public static string vblf = Environment.NewLine;

	public static string vbCrLf = Environment.NewLine;

	public const string ERROR_BAD_SYMBOLS = "ERROR_BAD_SYMBOLS";

	public const string ERROR_UNKNOWN = "ERROR_UNKNOWN";

	private NetScriptManager Manager = Globals.Manager;

	[DebuggerStepThrough]
	public string Hex(int num)
	{
		return num.ToString("X");
	}

	[DebuggerStepThrough]
	public string CStr(int num)
	{
		return Convert.ToString(num);
	}

	[DebuggerStepThrough]
	public string CStr(double num)
	{
		return Convert.ToString(num);
	}

	[DebuggerStepThrough]
	public string CStr(long num)
	{
		return Convert.ToString(num);
	}

	[DebuggerStepThrough]
	public string CStr(DateTime date)
	{
		return date.ToShortDateString();
	}

	[DebuggerStepThrough]
	public string CStr(bool expression)
	{
		if (expression)
		{
			return "True";
		}
		return "False";
	}

	[DebuggerStepThrough]
	public int CInt(string num)
	{
		if (int.TryParse(num, out var result))
		{
			return result;
		}
		throw new Exception("Cannot Convert String to Integer");
	}

	[DebuggerStepThrough]
	public int CInt(double num)
	{
		return (int)num;
	}

	[DebuggerStepThrough]
	public int CInt(long num)
	{
		return (int)num;
	}

	[DebuggerStepThrough]
	public char Chr(int charCode)
	{
		return (char)charCode;
	}

	[DebuggerStepThrough]
	public double CDbl(string num)
	{
		if (double.TryParse(num, out var result))
		{
			return result;
		}
		throw new Exception("Cannot Convert String To Double");
	}

	[DebuggerStepThrough]
	public double CDbl(int num)
	{
		return Convert.ToDouble(num);
	}

	[DebuggerStepThrough]
	public DateTime NowTypeSafe()
	{
		return DateTime.Now;
	}

	[DebuggerStepThrough]
	public string Now()
	{
		return DateTime.Now.ToString();
	}

	[DebuggerStepThrough]
	public int Day(string Date)
	{
		return Day(CDateTypeSafe(Date));
	}

	[DebuggerStepThrough]
	public int Day(DateTime Date)
	{
		return Date.Day;
	}

	[DebuggerStepThrough]
	public double Fix(double Number)
	{
		if (Number > -1.0)
		{
			return Math.Floor(Number);
		}
		Number *= -1.0;
		return Math.Floor(Number);
	}

	[DebuggerStepThrough]
	public DateTime DateAdd(string interval, int number, DateTime date)
	{
		switch (LCase(interval))
		{
		case "yyyy":
			return date.AddYears(number);
		case "q":
			return date.AddMonths(number * 3);
		case "m":
			return date.AddMonths(number);
		case "y":
		case "d":
		case "w":
			return date.AddDays(number);
		case "ww":
			return date.AddDays(number * 7);
		case "h":
			return date.AddHours(number);
		case "n":
			return date.AddMinutes(number);
		case "s":
			return date.AddSeconds(number);
		default:
			throw new Exception("Invalid interval detected");
		}
	}

	[DebuggerStepThrough]
	public DateTime DateAdd(string interval, int number, string date)
	{
		if (IsDate(date))
		{
			return DateAdd(interval, number, CDate(date));
		}
		throw new Exception("date passed is not a Valid DateTime object");
	}

	[DebuggerStepThrough]
	public bool IsDate(string DateExpression)
	{
		DateTime result = default(DateTime);
		return DateTime.TryParse(DateExpression, out result);
	}

	[DebuggerStepThrough]
	public DateTime CDateTypeSafe(string DateExpression)
	{
		DateTime result = default(DateTime);
		if (DateTime.TryParse(DateExpression, out result))
		{
			return result;
		}
		return default(DateTime);
	}

	[DebuggerStepThrough]
	public string CDate(string DateExpression)
	{
		DateTime result = default(DateTime);
		if (DateTime.TryParse(DateExpression, out result))
		{
			return result.ToString();
		}
		return "";
	}

	[DebuggerStepThrough]
	public int Round(double numToRound)
	{
		return Convert.ToInt32(Math.Round(numToRound));
	}

	[DebuggerStepThrough]
	public long CLng(double num)
	{
		return (long)num;
	}

	[DebuggerStepThrough]
	public long CLng(int num)
	{
		return num;
	}

	[DebuggerStepThrough]
	public long CLng(string num)
	{
		if (num == "" || num.Equals(string.Empty))
		{
			return 0L;
		}
		return long.Parse(num);
	}

	[DebuggerStepThrough]
	public string TypeName(object obj)
	{
		return obj.GetType().ToString();
	}

	[DebuggerStepThrough]
	public double FormatNumber(double Number, int NumDigAfterDec)
	{
		return Math.Round(Number, NumDigAfterDec);
	}

	[DebuggerStepThrough]
	public double FormatNumber(double Number)
	{
		return Math.Round(Number, 2);
	}

	[DebuggerStepThrough]
	public string Join(string[] arr, string delimeter)
	{
		if (arr.Count() == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string text in arr)
		{
			stringBuilder.Append(text + delimeter);
		}
		return stringBuilder.ToString();
	}

	[DebuggerStepThrough]
	public string Join(string[] arr)
	{
		if (arr.Count() == 0)
		{
			return string.Empty;
		}
		return Join(arr, string.Empty);
	}

	[DebuggerStepThrough]
	public string Trim(string str)
	{
		return str.Trim();
	}

	[DebuggerStepThrough]
	public void AddAnalysisError(int errorNumber, string errorSource, string errDescription, string strFunctionName)
	{
		string text = "Debug Diagnostic analysis script encountered the following errors while execution<br>";
		text = text + "Error Number: " + Convert.ToString(errorNumber) + "<br>Function Name: " + strFunctionName + "<br>Error Source:" + errorSource + " <br>Error Description: " + errDescription + " <br>";
		string text2 = "Note this represents a problem with the analysis script itself, not a problem with the process being analyzed.  Please let the <a href='mailto:dbgdiag@microsoft.com?Subject=Error in DebugDiag Analysis Script&Body=PLEASE COPY AND PASTE THE OUTPUT OF THE ERROR, AND ATTACH THE REPORT TO THE EMAIL. IF POSSIBLE PLEASE PROVIDE US A LINK TO DOWNLOAD THE DUMP FOR FURTHER REVIEW'>developers</a> of this script know about this error.";
		Globals.Manager.ReportOther(text, text2, "Notification", "notificationicon.png", 0, "{2599095b-e0d0-435c-8dad-3ade6a59c49f}");
	}

	[DebuggerStepThrough]
	public string LongNameFromShortName(string FileName)
	{
		return Path.GetFileName(FileName);
	}

	[DebuggerStepThrough]
	public string Mid(string str, int start)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		return str.Substring(start - 1);
	}

	[DebuggerStepThrough]
	public string Mid(string str, int start, int length)
	{
		if (str.Equals(string.Empty) || str == null)
		{
			return string.Empty;
		}
		return str.Substring(start - 1, length);
	}

	[DebuggerStepThrough]
	public int InStrRev(string FindIn, string ToFind, int StartIndex)
	{
		if (string.IsNullOrEmpty(FindIn))
		{
			return 0;
		}
		return FindIn.LastIndexOf(ToFind, StartIndex - 1) + 1;
	}

	[DebuggerStepThrough]
	public int InStrRev(int StartIndex, string FindIn, string ToFind)
	{
		if (string.IsNullOrEmpty(FindIn))
		{
			return 0;
		}
		return FindIn.LastIndexOf(ToFind, StartIndex - 1) + 1;
	}

	[DebuggerStepThrough]
	public int InStrRev(string FindIn, string ToFind)
	{
		if (string.IsNullOrEmpty(FindIn))
		{
			return 0;
		}
		return FindIn.LastIndexOf(ToFind) + 1;
	}

	[DebuggerStepThrough]
	public int InStr(int StartIndex, string FindIn, string ToFind)
	{
		if (string.IsNullOrEmpty(FindIn))
		{
			return 0;
		}
		return FindIn.IndexOf(ToFind, StartIndex - 1) + 1;
	}

	[DebuggerStepThrough]
	public int InStr(string FindIn, string ToFind)
	{
		if (string.IsNullOrEmpty(FindIn))
		{
			return 0;
		}
		return FindIn.IndexOf(ToFind) + 1;
	}

	[DebuggerStepThrough]
	public string MaskPwd(string ConnectionString)
	{
		bool flag = false;
		int num = 0;
		string text = "";
		if (InStr(UCase(ConnectionString), "PASSWORD") > 0)
		{
			num = InStr(UCase(ConnectionString), "PASSWORD");
		}
		else if (InStr(UCase(ConnectionString), "PWD") > 0)
		{
			num = InStr(UCase(ConnectionString), "PWD");
			flag = true;
		}
		if (num > 0)
		{
			int num2 = InStr(num + 1, ConnectionString, ";");
			if (num2 == 0)
			{
				num2 = Len(ConnectionString) + 1;
			}
			text = Mid(ConnectionString, 1, num - 1);
			text = ((!flag) ? (text + "Password=*****") : (text + "Pwd=*****"));
			return text + Mid(ConnectionString, num2, Len(ConnectionString) - num2 + 1);
		}
		return ConnectionString;
	}

	[DebuggerStepThrough]
	public string EndToggleSectionString()
	{
		return EndIndentedSectionString() + "</div>" + vbCrLf;
	}

	[DebuggerStepThrough]
	public void EndToggleSection()
	{
		EndIndentedSection();
		Globals.Manager.Write("</div>" + vbCrLf);
	}

	[DebuggerStepThrough]
	public void StartToggleSectionWithImage(string linkText, string key, string image, bool startCollapsed)
	{
		if (key == "")
		{
			key = ((!(Globals.g_UniqueReference != "")) ? GetCanonacolizedLinkKey(linkText) : (Globals.g_UniqueReference + ":" + GetCanonacolizedLinkKey(linkText)));
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = ((!startCollapsed) ? "ToggleStartExpanded" : "ToggleStartCollapsed");
		stringBuilder.Append("<b><a onclick='javascript:doToggle();return false;' id='");
		stringBuilder.Append(key);
		stringBuilder.Append("-t' class='" + text + "' style='cursor:hand; ");
		if (Len(image) > 0)
		{
			stringBuilder.Append("'>");
			stringBuilder.Append("<IMG class='" + text + "' align='bottom' src='");
			stringBuilder.Append(image);
			stringBuilder.Append("' id='");
			stringBuilder.Append(key);
			stringBuilder.Append("-i'> ");
			stringBuilder.Append(linkText);
		}
		else
		{
			stringBuilder.Append("'> ");
			stringBuilder.Append(linkText);
		}
		stringBuilder.Append("</a></b><br><div id='");
		stringBuilder.Append(key);
		stringBuilder.AppendLine("-s' style='DISPLAY: block'><br>");
		Globals.Manager.Write(stringBuilder.ToString());
		StartIndentedSection("");
	}

	[DebuggerStepThrough]
	public string GetCanonacolizedLinkKey(string key)
	{
		if (key != "")
		{
			key = Replace(key, " ", "_");
			key = Replace(key, "<", "_");
			key = Replace(key, ">", "_");
			key = Replace(key, "'", "_");
			key = Replace(key, "(", "_");
			key = Replace(key, ")", "_");
		}
		return key;
	}

	[DebuggerStepThrough]
	public string StartToggleSectionWithImageString(string linkText, string key, string image, bool startCollapsed)
	{
		if (key == "")
		{
			key = Globals.g_UniqueReference + ":" + GetCanonacolizedLinkKey(linkText);
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = ((!startCollapsed) ? "ToggleStartExpanded" : "ToggleStartCollapsed");
		stringBuilder.Append("<b><a onclick='javascript:doToggle();return false;' id='");
		stringBuilder.Append(key);
		stringBuilder.Append("-t' class='" + text + "' style='cursor:hand; ");
		if (Len(image) > 0)
		{
			stringBuilder.Append("'>");
			stringBuilder.Append("<IMG class='" + text + "' align='bottom' src='");
			stringBuilder.Append(image);
			stringBuilder.Append("' id='");
			stringBuilder.Append(key);
			stringBuilder.Append("-i'> ");
			stringBuilder.Append(linkText);
		}
		else
		{
			stringBuilder.Append("'> ");
			stringBuilder.Append(linkText);
		}
		stringBuilder.Append("</a></b><br><div id='");
		stringBuilder.Append(key);
		stringBuilder.AppendLine("-s' style='DISPLAY: block'><br>");
		return stringBuilder.ToString() + StartIndentedSectionString("");
	}

	[DebuggerStepThrough]
	public string StartToggleSectionString(string linkText, string key, bool startCollapsed)
	{
		return StartToggleSectionWithImageString(linkText, key, "res/up.png", startCollapsed);
	}

	[DebuggerStepThrough]
	public string StartToggleSectionWithHeaderString(string linkText, string key, bool startCollapsed, string headerLevel)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<br><br><br>");
		stringBuilder.Append(StartToggleSectionString(linkText, key, startCollapsed));
		stringBuilder.Append("<br>");
		stringBuilder.Append("<h" + headerLevel + "><a name='" + key + "'>" + linkText + "</a></h" + headerLevel + ">");
		return stringBuilder.ToString();
	}

	[DebuggerStepThrough]
	public void StartToggleSectionWithHeader(string linkText, string key, bool startCollapsed, int headerLevel)
	{
		StartToggleSectionWithHeader(linkText, key, startCollapsed, Convert.ToString(headerLevel));
	}

	[DebuggerStepThrough]
	public void StartToggleSectionWithHeader(string linkText, string key, bool startCollapsed, string headerLevel)
	{
		Globals.Manager.Write("<br><br><br>");
		Globals.Manager.Write("<h" + headerLevel + ">");
		StartToggleSection(linkText, key, startCollapsed);
		Globals.Manager.Write("<br>");
		Globals.Manager.Write("</h" + headerLevel + ">");
	}

	[DebuggerStepThrough]
	public void StartToggleSection(string linkText, string key, bool startCollapsed)
	{
		StartToggleSectionWithImage(linkText, key, "res/up.png", startCollapsed);
	}

	[DebuggerStepThrough]
	public string EndIndentedSectionString()
	{
		return "</td></tr></table>";
	}

	[DebuggerStepThrough]
	public void EndIndentedSection()
	{
		Globals.Manager.Write("</td></tr></table>");
	}

	[DebuggerStepThrough]
	public string StartIndentedSectionString(string width)
	{
		if (width == "")
		{
			width = "20px";
		}
		return "<table border=0 cellpadding=0 cellspacing=0 class=myCustomText width='100%'><tr><td width='" + width + "'></td><td>";
	}

	[DebuggerStepThrough]
	public void StartIndentedSection(string width)
	{
		if (width == "")
		{
			width = "20px";
		}
		Globals.Manager.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText width='100%'><tr><td width='" + width + "'></td><td>");
	}

	[DebuggerStepThrough]
	public void ShowTraceData()
	{
	}

	[DebuggerStepThrough]
	public void TRACE(string s)
	{
	}

	[DebuggerStepThrough]
	public double Max(double a, double b)
	{
		double result = b;
		if (a > b)
		{
			result = a;
		}
		return result;
	}

	[DebuggerStepThrough]
	public bool Not(bool condition)
	{
		return !condition;
	}

	[DebuggerStepThrough]
	public bool IsObject(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		return true;
	}

	[DebuggerStepThrough]
	public string GetUniqueReference(NetDbgObj Debugger)
	{
		return Convert.ToString(Debugger.ProcessID) + ":" + Convert.ToString(Debugger.ProcessUpTime);
	}

	[DebuggerStepThrough]
	public void CloseDebuggerForPid(int pid)
	{
	}

	public NetDbgObj OpenDebuggerForPid(int pid)
	{
		if (Globals.g_Debugger.ProcessID == pid)
		{
			return null;
		}
		LoadProcessesInThisReport();
		if (Globals.g_collProcessesInclucedInThisReport.ContainsKey(pid))
		{
			return Globals.Manager.GetDebugger(Globals.g_collProcessesInclucedInThisReport[pid]);
		}
		return null;
	}

	[DebuggerStepThrough]
	public void LoadProcessesInThisReport()
	{
		NetDbgObj val = null;
		if (Globals.g_collProcessesInclucedInThisReport.Count != 0)
		{
			return;
		}
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		if (dumpFiles.Count == 1)
		{
			return;
		}
		foreach (string item in dumpFiles)
		{
			if (!(UCase(Right(item, 4)) == ".DMP"))
			{
				continue;
			}
			NetDbgObj val2 = (val = Globals.Manager.GetDebugger(item));
			try
			{
				CacheFunctions.ResetCache();
				if (val != null && !Globals.g_collProcessesInclucedInThisReport.ContainsKey(val.ProcessID))
				{
					Globals.g_collProcessesInclucedInThisReport.Add(val.ProcessID, item);
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
	}

	[DebuggerStepThrough]
	public bool ProcessIsIncludedInThisReport(int pid)
	{
		if (Globals.g_Debugger.ProcessID == pid)
		{
			return true;
		}
		LoadProcessesInThisReport();
		return Globals.g_collProcessesInclucedInThisReport.ContainsKey(pid);
	}

	[DebuggerStepThrough]
	public string GetPercentageString(double percentageFraction, bool bBold, bool bCapital)
	{
		string text = "";
		text = ((percentageFraction == 0.0) ? "none" : ((percentageFraction != 1.0) ? ($"{percentageFraction * 100.0:N2}" + "%") : "all"));
		if (bCapital)
		{
			text = UCase(Left(text, 1)) + Right(text, Len(text) - 1);
		}
		if (bBold)
		{
			text = "<b>" + text + "</b>";
		}
		return text;
	}

	[DebuggerStepThrough]
	public string GetTrimmedThreadDescription(int ThreadNum)
	{
		if (Globals.g_AnalyzedThreads.Exists(ThreadNum))
		{
			return Globals.AnalyzeThreads.TrimLongDescription(Globals.g_AnalyzedThreads.Item(ThreadNum).Description);
		}
		return "Unknown";
	}

	[DebuggerStepThrough]
	public string IsAre(int count)
	{
		if (count == 1)
		{
			return " is ";
		}
		return "s are ";
	}

	[DebuggerStepThrough]
	public bool IsModuleLoaded(string moduleName)
	{
		if (Globals.g_ModuleCache.GetModuleByName(moduleName) == null)
		{
			return false;
		}
		return true;
	}

	[DebuggerStepThrough]
	public void DbgShowArray(string[] a)
	{
		TraceLine("DbgShowArray -->");
		for (int i = 0; i <= Globals.HelperFunctions.UBound(a); i++)
		{
			TraceLine(i + ": " + a[i]);
		}
		TraceLine("<---DbgShowArray");
	}

	[DebuggerStepThrough]
	public void DbgShowDebuggerExecute(string cmd)
	{
		TraceLine("DbgShowDebuggerExecute -->");
		TraceLine("cmd = " + cmd);
		TraceLine(DebuggerExecuteReplaceLF(cmd, "<BR>*"));
		TraceLine("<-- DbgShowDebuggerExecute");
	}

	[DebuggerStepThrough]
	public string DebuggerExecuteReplaceLF(string cmd, string replaceWith)
	{
		return Globals.g_Debugger.Execute(cmd).Replace("\r\n", replaceWith).Replace("\r", replaceWith)
			.Replace("\n", replaceWith);
	}

	[DebuggerStepThrough]
	public double FromHex(string theHexStr)
	{
		try
		{
			if (theHexStr.GetSafeLength() >= 2 && theHexStr.Substring(0, 2).ToLower() == "0x")
			{
				theHexStr = theHexStr.Substring(2);
			}
			theHexStr = theHexStr.Replace("\n", "");
			theHexStr = theHexStr.Replace("`", "");
			long result = 0L;
			long.TryParse(theHexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
			return result;
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	[DebuggerStepThrough]
	public double HexToDec(string strHex)
	{
		return long.Parse(strHex, NumberStyles.HexNumber);
	}

	[DebuggerStepThrough]
	public CacheFunctions.ScriptThreadClass GetThreadObjFromHexSystemID(double hexSystemID)
	{
		return Globals.g_ThreadInfoCache.ItemBySystemID(Convert.ToInt32(FromHex(Convert.ToString(hexSystemID))));
	}

	[DebuggerStepThrough]
	public string GetThreadIDWithLinkFromHexSystemID(double hexSystemTID)
	{
		return GetThreadIDWithLink(GetLogicalThreadNumFromSystemTID(FromHex(Convert.ToString(hexSystemTID))));
	}

	[DebuggerStepThrough]
	public string GetThreadIDWithLinkFromDecSystemID(double decSystemTID)
	{
		return GetThreadIDWithLink(GetLogicalThreadNumFromSystemTID(decSystemTID));
	}

	[DebuggerStepThrough]
	public string Spaces(int count)
	{
		string text = "";
		for (int i = 1; i <= count; i++)
		{
			text += "&nbsp";
		}
		return text;
	}

	[DebuggerStepThrough]
	public void TraceLine(string s)
	{
		Globals.Manager.Write("**TRACE** " + s + "<br>");
	}

	[DebuggerStepThrough]
	public bool FolderExists(string path)
	{
		return Directory.Exists(path);
	}

	[DebuggerStepThrough]
	public string GetAnalysisProcessEnvVar(string envVarName)
	{
		return Environment.GetEnvironmentVariable(envVarName);
	}

	[DebuggerStepThrough]
	public void SetOSVersion()
	{
		int num = 0;
		int num2 = 0;
		num = Globals.g_Debugger.OSVersionMajor;
		num2 = Globals.g_Debugger.OSVersionMinor;
		Globals.g_OSVER = Globals.OS_VER_UNKNOWN;
		if (num <= 4)
		{
			return;
		}
		switch (num)
		{
		case 5:
			switch (num2)
			{
			case 0:
				Globals.g_OSVER = Globals.OS_VER_WIN2K;
				break;
			case 1:
				Globals.g_OSVER = Globals.OS_VER_WINXP;
				break;
			case 2:
				if (Globals.g_Debugger.OSServicePack.GetSafeLength() > 0)
				{
					Globals.g_OSVER = Globals.OS_VER_WIN2K3SP;
				}
				else
				{
					Globals.g_OSVER = Globals.OS_VER_WIN2K3;
				}
				break;
			}
			break;
		case 6:
			switch (num2)
			{
			case 0:
				Globals.g_OSVER = Globals.OS_VER_WINVISTA;
				break;
			case 1:
				Globals.g_OSVER = Globals.OS_VER_WIN7;
				break;
			case 2:
				Globals.g_OSVER = Globals.OS_VER_WIN8;
				break;
			default:
				Globals.g_OSVER = Globals.OS_VER_WINNEW;
				break;
			}
			break;
		default:
			Globals.g_OSVER = Globals.OS_VER_WINNEW;
			break;
		}
		Globals.g_UtilExt = (Utils)Globals.g_Debugger.GetExtensionObject("CrashHangExt", "Utils");
		Globals.g_OSPlatformVersion = Globals.g_UtilExt.OSPlatformVersion;
		if (!Globals.g_Debugger.Is32Bit)
		{
			Globals.g_SizeOfULongPtr = 8;
		}
		else
		{
			Globals.g_SizeOfULongPtr = 4;
		}
		Globals.g_COMRuntimeModule = ((Globals.g_OSVER <= Globals.OS_VER_WIN7) ? "OLE32" : "COMBASE");
	}

	[DebuggerStepThrough]
	public int GetLogicalThreadNumFromSystemTID(double systemTID)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		int result = -1;
		if (systemTID > 0.0)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(Convert.ToInt32(systemTID));
			if (scriptThreadClass != null)
			{
				result = scriptThreadClass.ThreadID;
			}
		}
		return result;
	}

	[DebuggerStepThrough]
	public string EvaluateExpressionRaw(string Expression)
	{
		string text = "";
		string text2 = null;
		int num = 0;
		string text3 = "";
		string[] array = null;
		int num2 = 0;
		text = "ERROR_UNKNOWN";
		text2 = Globals.g_Debugger.Execute("? " + Expression);
		if (text2 != "")
		{
			string[] array2 = Split(Convert.ToString(text2), "\n");
			for (num = Globals.HelperFunctions.UBound(array2); num >= 0; num += -1)
			{
				text3 = array2[num];
				if (text3.Substring(0, 22).ToUpper() == "COULDN'T RESOLVE ERROR")
				{
					return Convert.ToString("ERROR_BAD_SYMBOLS");
				}
				if (!(text3.Substring(0, 19).ToUpper() == "EVALUATE EXPRESSION"))
				{
					continue;
				}
				array = Split(text3, " ");
				num2 = Globals.HelperFunctions.UBound(array);
				if (num2 > 1)
				{
					text = array[num2];
					if (text.GetSafeLength() == 8)
					{
						text = Convert.ToString(Convert.ToInt64("&H" + text));
					}
					break;
				}
			}
		}
		return text;
	}

	[DebuggerStepThrough]
	public string EvaluateExpressionNoErrors(string Expression)
	{
		string text = "";
		text = EvaluateExpressionRaw(Expression);
		if (text == "ERROR_BAD_SYMBOLS" || text == "ERROR_UNKNOWN")
		{
			text = "";
		}
		return text;
	}

	[DebuggerStepThrough]
	public string EvaluateExpressionAndReportErrors(string Expression)
	{
		string text = "";
		string text2 = "";
		text = EvaluateExpressionRaw(Expression);
		if (text == Convert.ToString("ERROR_BAD_SYMBOLS"))
		{
			text = "";
			if (Globals.g_SymErrDisplayed)
			{
				return text;
			}
			Globals.g_SymErrDisplayed = true;
			text2 = "Symbols were not available to evaluate the expression \"" + Convert.ToString(Expression) + "\".  The COM+ STA ThreadPool report may not be accurate. Ensure that a symbol path is configured, enabling the proper symbols to be loaded.<br><br>To modify or view the current symbol path from within DebugDiag, select <b>Options and Settings</b> from the <b>Tools</b> menu. If symbols aren't available and/or a proper symbol path is already setup, then these threads should be manually reviewed further to determine what they are doing. For more information regarding debug symbols for Microsoft components, please visit the following site:<br><br><a target=_new href='http://www.microsoft.com/whdc/devtools/debugging/debugstart.mspx'>Debugging Tools and Symbols: Getting Started</a><br>";
			Globals.Manager.ReportInformation(text2, 0, "{a5ba206b-21c0-49da-9943-4431282716bf}");
		}
		return text;
	}

	[DebuggerStepThrough]
	public string GetDwordAtSymbolAndReportErrors(string symbol)
	{
		string text = "0";
		string text2 = "";
		text = GetDwordAtSymbolRaw(symbol);
		if (text == "ERROR_BAD_SYMBOLS")
		{
			text = "0";
			if (Globals.g_SymErrDisplayed)
			{
				return text;
			}
			Globals.g_SymErrDisplayed = true;
			text2 = "Symbols were not available to resolve the symbol \"" + Convert.ToString(symbol) + "\".  The COM+ STA ThreadPool report may not be accurate. ";
			string text3 = "Ensure that a symbol path is configured, enabling the proper symbols to be loaded.<br><br>To modify or view the current symbol path from within DebugDiag, select <b>Options and Settings</b> from the <b>Tools</b> menu. If symbols aren't available and/or a proper symbol path is already setup, then these threads should be manually reviewed further to determine what they are doing. For more information regarding debug symbols for Microsoft components, please visit the following site:<br><br><a target=_new href='http://www.microsoft.com/whdc/devtools/debugging/debugstart.mspx'>Debugging Tools and Symbols: Getting Started</a><br>";
			Globals.Manager.ReportOther(text2, text3, "Notification", "notificationicon.png", 0, "{f412c26e-a7ad-489a-9c5b-30cb832f9d15}");
		}
		else if (text == "ERROR_UNKNOWN")
		{
			text = "0";
			if (Globals.g_SymErrDisplayed)
			{
				return text;
			}
			Globals.g_SymErrDisplayed = true;
			text2 = "An unknown error occured while attempting to get the value of \"" + Convert.ToString(symbol) + "\".  The COM+ STA ThreadPool report may not be accurate. This may be an internal problem with the DebugDiag script itself, not a problem with the dump file being analyzed.  Please follow up with Microsoft Support";
			Globals.Manager.ReportInformation(text2, 0, "{9a1b4a82-846f-424a-9ed9-4629004b2982}");
		}
		return text;
	}

	[DebuggerStepThrough]
	public string GetDwordAtSymbolNoErrors(string symbol)
	{
		string dwordAtSymbolRaw = GetDwordAtSymbolRaw(symbol);
		if (dwordAtSymbolRaw == "ERROR_BAD_SYMBOLS" || dwordAtSymbolRaw == "ERROR_UNKNOWN")
		{
			return "0";
		}
		return dwordAtSymbolRaw;
	}

	[DebuggerStepThrough]
	public string GetDwordAtSymbolRaw(string symbol)
	{
		string text = "dd " + symbol + " l1";
		Globals.g_Debugger.Execute("!sym quiet");
		string text2 = Globals.g_Debugger.Execute(text);
		string[] array = Split(text2, "  ");
		if (UBound(array) == 1)
		{
			return Convert.ToString(FromHex(array[1]));
		}
		if (LCase(Left(text2, Len("Couldn't resolve error at"))) == LCase("Couldn't resolve error at"))
		{
			return "ERROR_BAD_SYMBOLS";
		}
		return "ERROR_UNKNOWN";
	}

	[DebuggerStepThrough]
	public string GetQwordAtSymbolAndReportErrors(string symbol)
	{
		string text = "0";
		string text2 = "";
		text = GetQwordAtSymbolRaw(symbol);
		if (text == "ERROR_BAD_SYMBOLS")
		{
			text = "0";
			if (Globals.g_SymErrDisplayed)
			{
				return text;
			}
			Globals.g_SymErrDisplayed = true;
			text2 = "Symbols were not available to resolve the symbol \"" + Convert.ToString(symbol) + "\".  The COM+ STA ThreadPool report may not be accurate. Ensure that a symbol path is configured, enabling the proper symbols to be loaded.<br><br>To modify or view the current symbol path from within DebugDiag, select <b>Options and Settings</b> from the <b>Tools</b> menu. If symbols aren't available and/or a proper symbol path is already setup, then these threads should be manually reviewed further to determine what they are doing. For more information regarding debug symbols for Microsoft components, please visit the following site:<br><br><a target=_new href='http://www.microsoft.com/whdc/devtools/debugging/debugstart.mspx'>Debugging Tools and Symbols: Getting Started</a><br>";
			Globals.Manager.ReportInformation(text2, 0, "{f412c26e-a7ad-489a-9c5b-30cb832f9d15}");
		}
		else if (text == "ERROR_UNKNOWN")
		{
			text = "0";
			if (Globals.g_SymErrDisplayed)
			{
				return text;
			}
			Globals.g_SymErrDisplayed = true;
			text2 = "An unknown error occured while attempting to get the value of \"" + Convert.ToString(symbol) + "\".  The COM+ STA ThreadPool report may not be accurate. This may be an internal problem with the DebugDiag script itself, not a problem with the dump file being analyzed.  Please follow up with Microsoft Support";
			Globals.Manager.ReportInformation(text2, 0, "{9a1b4a82-846f-424a-9ed9-4629004b2982}");
		}
		return text;
	}

	[DebuggerStepThrough]
	public string GetQwordAtSymbolNoErrors(string symbol)
	{
		string qwordAtSymbolRaw = GetQwordAtSymbolRaw(symbol);
		if (qwordAtSymbolRaw == "ERROR_BAD_SYMBOLS" || qwordAtSymbolRaw == "ERROR_UNKNOWN")
		{
			return "0";
		}
		return qwordAtSymbolRaw;
	}

	[DebuggerStepThrough]
	public string GetQwordAtSymbolRaw(string symbol)
	{
		string text = "dq " + symbol + " l1";
		Globals.g_Debugger.Execute("!sym quiet");
		string text2 = Globals.g_Debugger.Execute(text);
		string[] array = Split(text2, "  ");
		if (UBound(array) == 1)
		{
			return Convert.ToString(FromHex(array[1]));
		}
		if (LCase(Left(text2, Len("Couldn't resolve error at"))) == LCase("Couldn't resolve error at"))
		{
			return "ERROR_BAD_SYMBOLS";
		}
		return "ERROR_UNKNOWN";
	}

	[DebuggerStepThrough]
	public int UBound(object[] sa)
	{
		return sa?.GetUpperBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int UBound(string[] sa)
	{
		return sa?.GetUpperBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int UBound_HACK_DO_NOT_USE(Array array, int dimension)
	{
		return array?.GetUpperBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int UBound(Array array)
	{
		return array?.GetUpperBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int LBound(object[] sa)
	{
		return sa?.GetLowerBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int LBound(string[] sa)
	{
		return sa?.GetLowerBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int LBound_HACK_DO_NOT_USE(Array array, int dimension)
	{
		return array?.GetLowerBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int LBound(Array array)
	{
		return array?.GetLowerBound(0) ?? (-1);
	}

	[DebuggerStepThrough]
	public int Len(string str)
	{
		return str.GetSafeLength();
	}

	[DebuggerStepThrough]
	public string Left(string str, int count)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		if (count > str.GetSafeLength())
		{
			count = str.GetSafeLength();
		}
		return str.Substring(0, count);
	}

	[DebuggerStepThrough]
	public string Right(string str, int count)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		if (count > str.GetSafeLength())
		{
			count = str.GetSafeLength();
		}
		return str.Substring(str.GetSafeLength() - count);
	}

	[DebuggerStepThrough]
	public string UCase(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		return str.ToUpper();
	}

	[DebuggerStepThrough]
	public string EvaluateSymbolNoErrors(string symbol)
	{
		string[] array = Globals.g_Debugger.Execute("? " + Convert.ToString(symbol)).Split(' ');
		if (Globals.HelperFunctions.UBound(array) == 4)
		{
			return Convert.ToString(FromHex(array[4]));
		}
		return "";
	}

	[DebuggerStepThrough]
	public string GetAs32BitHexString(double address)
	{
		return Globals.g_Debugger.GetAs32BitHexString(Globals.g_Debugger.ReadDWord(address));
	}

	[DebuggerStepThrough]
	public string GetAs64BitHexString(double address)
	{
		return Globals.g_Debugger.GetAs64BitHexString(Globals.g_Debugger.ReadQWord(address));
	}

	[DebuggerStepThrough]
	public double ReadULongPtr(double address)
	{
		if (Globals.g_SizeOfULongPtr == 8)
		{
			return Globals.g_Debugger.ReadQWord(address);
		}
		return Globals.g_Debugger.ReadDWord(address);
	}

	[DebuggerStepThrough]
	public string GetArgAsHexString(CacheFunctions.ScriptStackFrameClass StackFrame, int nZeroBasedArgNum)
	{
		return GetAsHexString(StackFrame.ChildEBP + (double)(2 * Globals.g_SizeOfULongPtr) + (double)(Globals.g_SizeOfULongPtr * nZeroBasedArgNum));
	}

	[DebuggerStepThrough]
	public void ReportModuleInfo()
	{
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		scriptModuleClass = CacheFunctions.GetModuleFromAddress(Globals.g_Debugger.NativeException.ExceptionAddress);
		if (scriptModuleClass != null)
		{
			ModuleInfo(scriptModuleClass);
		}
	}

	[DebuggerStepThrough]
	public string CheckSymbolType(double ModuleBase)
	{
		IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(ModuleBase);
		string text;
		string text2;
		if (moduleByAddress == null)
		{
			string[] array = Globals.g_Debugger.GetSymbolFromAddress(ModuleBase).Split('+');
			if (array[0].ToUpper().IndexOf("UNLOADED") > -1)
			{
				int safeLength = array[0].GetSafeLength();
				text = array[0].Substring(2, safeLength - 2) + ":1";
				text2 = "UNKNOWN";
			}
			else
			{
				text = "UNKNOWN_MODULE:0";
				text2 = "UNKNOWN";
			}
		}
		else
		{
			int num = moduleByAddress.ImageName.LastIndexOf('\\');
			int safeLength2 = moduleByAddress.ImageName.GetSafeLength();
			text = moduleByAddress.ImageName.Substring(num, safeLength2 - num) + ":0";
			text2 = moduleByAddress.SymbolType.ToUpper();
		}
		switch (text2)
		{
		case "UNKNOWN":
		case "EXPORT":
		case "NONE":
			return text + ":0";
		default:
			return text + ":1";
		}
	}

	[DebuggerStepThrough]
	public void ClearSubStatus()
	{
		ResetStatus("", 0, "");
	}

	[DebuggerStepThrough]
	public void IncrementSubStatus()
	{
		Globals.g_SubStatusPosition++;
		Globals.g_Progress.CurrentPosition = Globals.g_SubStatusPosition;
		if (Globals.g_subStatusTitle.GetSafeLength() > 1)
		{
			Globals.g_Progress.CurrentStatus = Globals.g_subStatusTitle + " " + Globals.g_SubStatusPosition + " of " + Globals.g_SubStatusMaxPosition;
		}
	}

	[DebuggerStepThrough]
	public void ResetStatusNoIncrement(string caption)
	{
		Globals.g_Progress.OverallStatus = caption;
		Globals.g_SubStatusCaption = "";
		Globals.g_Progress.CurrentStatus = Globals.g_SubStatusCaption;
		Globals.g_Progress.SetCurrentRange(0, 2);
		Globals.g_SubStatusPosition = 1;
		Globals.g_Progress.CurrentPosition = Globals.g_SubStatusPosition;
	}

	[DebuggerStepThrough]
	public void ResetStatus(string caption, int maxProgress, string subStatusTitle)
	{
		Globals.g_Progress.OverallStatus = caption;
		Globals.g_Progress.CurrentStatus = caption;
		Globals.g_SubStatusCaption = caption;
		Globals.g_subStatusTitle = subStatusTitle;
		Globals.g_SubStatusMaxPosition = maxProgress;
		if (maxProgress == 1)
		{
			Globals.g_SubStatusPosition = 0;
			Globals.g_Progress.SetCurrentRange(0, 2);
		}
		else
		{
			Globals.g_SubStatusPosition = -1;
			Globals.g_Progress.SetCurrentRange(0, maxProgress);
		}
		IncrementSubStatus();
	}

	[DebuggerStepThrough]
	public void UpdateOverallProgress()
	{
		Globals.g_OverallProgress++;
		Globals.g_Progress.OverallPosition = Globals.g_OverallProgress;
		ClearSubStatus();
	}

	[DebuggerStepThrough]
	public void GenerateReportHeader(string DataFile, string DumpType)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected O, but got Unknown
		ReportSection currentSection = Globals.Manager.CurrentSection;
		int num = 0;
		object obj = null;
		int num2 = 0;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		HTMLTable val = new HTMLTable();
		val.AddRow(new object[2] { "Type of Analysis Performed", DumpType });
		val.AddRow(new object[2]
		{
			"Machine Name",
			Globals.g_Debugger.EnvironmentVariables[(object)"ComputerName"]
		});
		val.AddRow(new object[2]
		{
			"Operating System",
			Globals.g_Debugger.OSVersion + Globals.g_Debugger.OSServicePack
		});
		val.AddRow(new object[2]
		{
			"Number Of Processors",
			Globals.g_Debugger.EnvironmentVariables[(object)"Number_Of_Processors"]
		});
		val.AddRow(new object[2]
		{
			"Process ID",
			Convert.ToString(Globals.g_Debugger.ProcessID)
		});
		val.AddRow(new object[2]
		{
			"Process Image",
			Globals.g_Debugger.ExecutableName
		});
		val.AddRow(new object[2]
		{
			"Command Line",
			Globals.g_Debugger.CommandLine
		});
		val.AddRow(new object[2]
		{
			"System Up-Time",
			PrintTime(Globals.g_Debugger.SystemUpTime)
		});
		val.AddRow(new object[2]
		{
			"Process Up-Time",
			PrintTime(Globals.g_Debugger.ProcessUpTime)
		});
		val.AddRow(new object[2]
		{
			"Processor Type",
			Globals.g_OSPlatformVersion
		});
		val.AddRow(new object[2]
		{
			"Process Bitness",
			ReturnProcessBitness()
		});
		currentSection.Write(((object)val).ToString());
		if (!(Convert.ToString(DumpType) == "Hang Analysis") && !(Convert.ToString(DumpType) == "Combined Crash/Hang Analysis"))
		{
			return;
		}
		ReportSection val2 = Globals.Manager.AddReportSection("CPU", (SectionType)0);
		val2.Title = "Top 5 Threads by CPU time";
		val = new HTMLTable("CPUTable");
		val.AddColumns(new object[3] { "Thread ID", "Total CPU Time", "Entry Point for Thread" });
		if (Globals.g_Debugger.ExtendedThreadInfoAvailable)
		{
			num2 = ((Globals.g_ThreadInfoCache.Count >= 5) ? 5 : Globals.g_ThreadInfoCache.Count);
			dynamic threadInfoByCPUTimes = Globals.g_UtilExt.ThreadInfoByCPUTimes;
			for (num = 0; num <= num2 - 1; num++)
			{
				scriptThreadClass = Globals.g_ThreadInfoCache.Item(threadInfoByCPUTimes[num, 0]);
				obj = PadZero2(Convert.ToInt32(threadInfoByCPUTimes[num, 1]) % 1000);
				val.AddRow(GetThreadIDWithLink(threadInfoByCPUTimes[num, 0]), PrintTime(Convert.ToInt32(threadInfoByCPUTimes[num, 1]) / 1000) + "." + Convert.ToString(obj), CacheFunctions.GetSymbolFromAddress(scriptThreadClass.StartAddress));
			}
			val2.Write(((object)val).ToString());
			val2.Write("<br><b>Note</b> - Times include both user mode and kernel mode for each thread\r\n");
		}
		else
		{
			val2.Write("<br><b>Extended thread information (including thread CPU times) is unavailable in this dump</b>");
		}
	}

	[DebuggerStepThrough]
	public string ReturnProcessBitness()
	{
		if (Globals.g_SizeOfULongPtr == 4)
		{
			return "32-Bit";
		}
		if (Globals.g_SizeOfULongPtr == 8)
		{
			return "64-Bit";
		}
		return "Unknown";
	}

	[DebuggerStepThrough]
	public string GetASPTemplateWithLink(IASPTemplate ASPTemplate)
	{
		return "<a href='#" + Globals.g_UniqueReference + "ASPTemplate" + ASPTemplate.ApplicationURL + ASPTemplate.PhysicalPath + "'><b>" + ASPTemplate.PhysicalPath + "</b></a>";
	}

	[DebuggerStepThrough]
	public string GetASPAppWithLink(IASPApplication ASPApp)
	{
		return "<a href='#" + Globals.g_UniqueReference + "ASPApp" + ASPApp.MetabaseKey + "'><b>" + ASPApp.MetabaseKey + "</b></a>";
	}

	[DebuggerStepThrough]
	public string GetCritSecWithLink(string CritSecAddress)
	{
		return "<a href='#" + Globals.g_UniqueReference + "CritSec" + CritSecAddress + "'><b>" + CacheFunctions.GetSymbolFromAddress(double.Parse(CritSecAddress)) + "</b></a>";
	}

	[DebuggerStepThrough]
	public string GetCritSecWithLink(double CritSecAddress)
	{
		return "<a href='#" + Globals.g_UniqueReference + "CritSec" + CritSecAddress + "'><b>" + CacheFunctions.GetSymbolFromAddress(CritSecAddress) + "</b></a>";
	}

	[DebuggerStepThrough]
	public string GetThreadAndProcessIDWithLinkOOP(NetDbgObj Debugger, int SystemThreadID)
	{
		string text = "";
		if (Debugger != null)
		{
			if (SystemThreadID > -1)
			{
				foreach (NetDbgThread thread in Globals.g_Debugger.Threads)
				{
					if (thread.ThreadID == SystemThreadID)
					{
						text = GetUniqueReference(Debugger);
						return "thread <a href='#" + text + "Thread" + thread.SystemID + "'><b>" + thread.ThreadID + "</b></a> in process " + Debugger.ProcessID;
					}
				}
				return "thread with system id " + SystemThreadID + " in <a href='#Dump" + text + "-t'><b>process" + Debugger.ProcessID + "</b></a>";
			}
			return "thread with system id " + SystemThreadID + " in <a href='#Dump" + text + "-t'><b>process" + Debugger.ProcessID + "</b></a>";
		}
		return "thread with system id " + SystemThreadID + " </b> in an unknown process";
	}

	[DebuggerStepThrough]
	public string GetThreadIDWithLink(int ThreadID)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		scriptThreadClass = null;
		if (ThreadID > -1)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(ThreadID);
		}
		if (scriptThreadClass == null)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(0);
		}
		return "<a href='#" + Convert.ToString(Globals.g_UniqueReference) + "Thread" + Convert.ToString(scriptThreadClass.SystemID) + "'><b>" + Convert.ToString(scriptThreadClass.ThreadID) + "</b></a>";
	}

	[DebuggerStepThrough]
	public string PrintMemory(double Memory)
	{
		string text = "";
		if (Memory > Math.Pow(1024.0, 4.0))
		{
			return "<font color=Crimson>" + Convert.ToString(Math.Round(Memory / Math.Pow(1024.0, 4.0), 2)) + " TBytes</font>";
		}
		if (Memory > Math.Pow(1024.0, 3.0))
		{
			return "<font color=DarkRed>" + Convert.ToString(Math.Round(Memory / Math.Pow(1024.0, 3.0), 2)) + " GBytes</font>";
		}
		if (Memory > Math.Pow(1024.0, 2.0))
		{
			return "<font color=SaddleBrown>" + Convert.ToString(Math.Round(Memory / Math.Pow(1024.0, 2.0), 2)) + " MBytes</font>";
		}
		if (Memory > 1024.0)
		{
			return "<font color=DarkGreen>" + Convert.ToString(Math.Round(Memory / 1024.0, 2)) + " KBytes</font>";
		}
		return "<font color=DarkGreen>" + Convert.ToString(Math.Round(Memory)) + " Bytes</font>";
	}

	[DebuggerStepThrough]
	public string PrintTime3(double MilliSeconds)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		num = MilliSeconds / 86400000.0;
		MilliSeconds %= 86400000.0;
		num2 = MilliSeconds / 3600000.0;
		MilliSeconds %= 3600000.0;
		num3 = MilliSeconds / 60000.0;
		MilliSeconds %= 60000.0;
		num4 = MilliSeconds / 1000.0;
		MilliSeconds %= 1000.0;
		return PrintTime2(num, num2, num3, num4, MilliSeconds);
	}

	[DebuggerStepThrough]
	public string PrintTime2(double Days, double Hours, double Minutes, double Seconds, double MiliSeconds)
	{
		string text = "";
		if (Days >= 1.0)
		{
			text = Convert.ToInt32(Days) + " day(s) ";
		}
		text = text + Convert.ToString(PadZero(Convert.ToInt32(Math.Floor(Hours)))) + ":" + Convert.ToString(PadZero(Convert.ToInt32(Math.Floor(Minutes)))) + ":" + Convert.ToString(PadZero(Convert.ToInt32(Math.Floor(Seconds))));
		if (MiliSeconds > 0.0)
		{
			text = text + "." + Convert.ToString(PadZero2(Convert.ToInt32(MiliSeconds)));
		}
		return text;
	}

	[DebuggerStepThrough]
	public string PrintTime(double Seconds)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		num = Seconds / 86400.0;
		Seconds %= 86400.0;
		num2 = Seconds / 3600.0;
		Seconds %= 3600.0;
		num3 = Seconds / 60.0;
		Seconds %= 60.0;
		return PrintTime2(num, num2, num3, Seconds, 0.0);
	}

	[DebuggerStepThrough]
	public string GetGUIDString(string pGUID)
	{
		int num = 0;
		string[] array = new string[4];
		for (num = 0; num <= 3; num++)
		{
			if (Globals.g_SizeOfULongPtr == 8)
			{
				array[num] = Convert.ToString(Globals.g_Debugger.GetAs32BitHexString(Globals.g_Debugger.ReadDWord((double)(Convert.ToInt64(pGUID) + num * 4)))).ToUpper().Replace("0X", "");
			}
			else
			{
				array[num] = Convert.ToString(Globals.g_Debugger.GetAs32BitHexString(Globals.g_Debugger.ReadDWord((double)(Convert.ToInt32(pGUID) + num * 4)))).ToUpper().Replace("0X", "");
			}
		}
		return "{" + array[0] + "-" + array[1].Substring(array[1].GetSafeLength() - 4) + "-" + array[1].Substring(0, 4) + "-" + array[2].Substring(array[2].GetSafeLength() - 2) + array[2].Substring(array[2].GetSafeLength() - 4).Substring(0, 2) + "-" + array[2].Substring(0, 4).Substring(array[2].Substring(0, 4).GetSafeLength() - 2) + array[2].Substring(0, 2) + array[3].Substring(array[3].GetSafeLength() - 2) + array[3].Substring(array[3].GetSafeLength() - 4).Substring(0, 2) + array[3].Substring(0, 4).Substring(array[3].Substring(0, 4).GetSafeLength() - 2) + array[3].Substring(0, 2) + "}";
	}

	[DebuggerStepThrough]
	public double Min(double a, double b)
	{
		if (!(a < b))
		{
			return b;
		}
		return a;
	}

	[DebuggerStepThrough]
	public string PadZero(int a)
	{
		string text = Convert.ToString(a);
		if (a < 10)
		{
			text = "0" + text;
		}
		return text;
	}

	[DebuggerStepThrough]
	public string PadZero2(int a)
	{
		string text = Convert.ToString(a);
		if (a < 10)
		{
			text = "00" + text;
		}
		else if (a < 100)
		{
			text = "0" + text;
		}
		return text;
	}

	[DebuggerStepThrough]
	public string GetAsHexString(double address)
	{
		double num = ReadULongPtr(address);
		if (Globals.g_SizeOfULongPtr == 8)
		{
			return Globals.g_Debugger.GetAs64BitHexString(num);
		}
		return Globals.g_Debugger.GetAs32BitHexString(num);
	}

	[DebuggerStepThrough]
	public string[] Split(string Expression, string Delimiter)
	{
		return Expression.Split(new string[1] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
	}

	[DebuggerStepThrough]
	public string[] Split(string Expression, string Delimiter, int Count)
	{
		if (Count == -1)
		{
			Count = 1000;
		}
		return Expression.Split(Delimiter.ToCharArray(), Count);
	}

	[DebuggerStepThrough]
	public string[] Split(string Expression, string Delimiter, int Count, int CompareMode)
	{
		return Split(Expression, Delimiter, Count);
	}

	[DebuggerStepThrough]
	public string Replace(string stringToPass, string find, string replaceWith)
	{
		return stringToPass.Replace(find, replaceWith);
	}

	[DebuggerStepThrough]
	public string LCase(string stringToCovert)
	{
		return stringToCovert.ToLower();
	}

	[DebuggerStepThrough]
	public void ModuleInfo(CacheFunctions.ScriptModuleClass Module)
	{
		ReportSection currentSection = Globals.Manager.CurrentSection;
		currentSection.Write("<table cellpadding=0 cellspacing=0 border=0 class='myCustomText tableClass' ID='Table2' data-debug-type=\"table\">");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<td nowrap><h2><a name='" + Globals.g_UniqueReference + "Module'>Module Information</a></h2></td>");
		currentSection.Write("</tr>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Image Name:</b></TD>");
		currentSection.Write("<td>" + Module.ImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Symbol Type:</b> </TD>");
		currentSection.Write("<td>" + Module.SymbolType + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Base address:</b></TD>");
		currentSection.Write("<td>" + GetAsHexString(Module.Base) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Time Stamp:</b> </TD>");
		currentSection.Write("<td>" + Module.TimeStamp + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Checksum:</b></TD>");
		currentSection.Write("<td>" + GetAsHexString(Module.CheckSum) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Comments:</b> </TD><td>" + Module.VSComments + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>COM DLL:</b></TD><td>" + Module.IsCOMDLL + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Company Name:</b> </TD><td>" + Module.VSCompanyName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>ISAPIExtension:</b></TD><td>" + Module.IsISAPIExtension + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>File Description:</b> </TD><td>" + Module.VSFileDescription + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>ISAPIFilter:</b></TD><td>" + Module.IsISAPIFilter + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>File Version:</b> </TD><td>" + Module.VSFileVersion + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Managed DLL:</b></TD><td>" + Module.IsManaged + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Internal Name:</b> </TD><td>" + Module.VSInternalName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>VB DLL:</b></TD><td>" + Module.IsVBModule + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Legal Copyright:</b> </TD><td>" + Module.VSLegalCopyright + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Loaded Image Name:</b> </TD><td>" + Module.LoadedImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Legal Trademarks:</b> </TD><td>" + Module.VSLegalTrademarks + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Mapped Image Name:</b> </TD><td>" + Module.MappedImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Original filename:</b> </TD><td>" + Module.VSOriginalFilename + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Module name:</b> </TD><td>" + Module.ModuleName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Private Build:</b> </TD><td>" + Module.VSPrivateBuild + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Single Threaded:</b> </TD><td>" + Module.SingleThreaded + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Product Name:</b> </TD><td>" + Module.VSProductName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Module Size:</b> </TD><td>" + PrintMemory(Module.Size) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Product Version:</b> </TD><td>" + Module.VSProductVersion + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Symbol File Name:</b> </TD><td>" + Module.SymbolFileName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Special Build:</b> </TD><td>&" + Module.VSSpecialBuild + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("</table>");
		currentSection.Write("<br><br>");
	}

	[DebuggerStepThrough]
	public void ModuleInfo(IDbgModule Module)
	{
		ReportSection currentSection = Globals.Manager.CurrentSection;
		currentSection.Write("<table cellpadding=0 cellspacing=0 border=0 class='myCustomText tableClass' ID='Table2' data-debug-type=\"table\">");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<td nowrap><h2><a name='" + Globals.g_UniqueReference + "Module'>Module Information</a></h2></td>");
		currentSection.Write("</tr>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Image Name:</b></TD>");
		currentSection.Write("<td>" + Module.ImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Symbol Type:</b> </TD>");
		currentSection.Write("<td>" + Module.SymbolType + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Base address:</b></TD>");
		currentSection.Write("<td>" + GetAsHexString(Module.Base) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Time Stamp:</b> </TD>");
		currentSection.Write("<td>" + Module.TimeStamp + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Checksum:</b></TD>");
		currentSection.Write("<td>" + GetAsHexString(Module.Checksum) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Comments:</b> </TD><td>" + Module.VSComments + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>COM DLL:</b></TD><td>" + Module.IsCOMDLL + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Company Name:</b> </TD><td>" + Module.VSCompanyName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>ISAPIExtension:</b></TD><td>" + Module.IsISAPIExtension + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>File Description:</b> </TD><td>" + Module.VSFileDescription + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>ISAPIFilter:</b></TD><td>" + Module.IsISAPIFilter + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>File Version:</b> </TD><td>" + Module.VSFileVersion + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Managed DLL:</b></TD><td>" + Module.IsManaged + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Internal Name:</b> </TD><td>" + Module.VSInternalName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>VB DLL:</b></TD><td>" + Module.IsVBModule + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Legal Copyright:</b> </TD><td>" + Module.VSLegalCopyright + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Loaded Image Name:</b> </TD><td>" + Module.LoadedImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Legal Trademarks:</b> </TD><td>" + Module.VSLegalTrademarks + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Mapped Image Name:</b> </TD><td>" + Module.MappedImageName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Original filename:</b> </TD><td>" + Module.VSOriginalFilename + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Module name:</b> </TD><td>" + Module.ModuleName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Private Build:</b> </TD><td>" + Module.VSPrivateBuild + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Single Threaded:</b> </TD><td>" + Module.SingleThreaded + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Product Name:</b> </TD><td>" + Module.VSProductName + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Module Size:</b> </TD><td>" + PrintMemory(Module.Size) + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Product Version:</b> </TD><td>" + Module.VSProductVersion + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("<tr data-is-row=\"true\">");
		currentSection.Write("<TD><b>Symbol File Name:</b> </TD><td>" + Module.SymbolFileName + "</td>");
		currentSection.Write("<TD>&nbsp;&nbsp;<b>Special Build:</b> </TD><td>&" + Module.VSSpecialBuild + "</td>");
		currentSection.Write("</TR>");
		currentSection.Write("</table>");
		currentSection.Write("<br><br>");
	}

	[DebuggerStepThrough]
	public string GetVendorMessage(double ModuleAddress)
	{
		string result = null;
		CacheFunctions.ScriptModuleClass moduleFromAddress = CacheFunctions.GetModuleFromAddress(ModuleAddress);
		if (moduleFromAddress != null)
		{
			result = ((!(moduleFromAddress.ModuleName.ToUpper() != "VBSCRIPT") || !(moduleFromAddress.ModuleName.ToUpper() != "JSCRIPT")) ? "<br><br>The call originates from script code.  Please review the script code to identify the source of the message box." : ((!(moduleFromAddress.VSCompanyName != "")) ? ("<br><br>Please follow up with the vendor for the file <b>" + moduleFromAddress.ImageName + "</b> for problem resolution.") : ("<br><br>Please follow up with vendor <b>" + moduleFromAddress.VSCompanyName + "</b> for problem resolution concerning the following file: <b>" + moduleFromAddress.ImageName + ".</b>")));
		}
		return result;
	}

	[DebuggerStepThrough]
	public double GetDirectCaller(Dictionary<int, CacheFunctions.ScriptStackFrameClass> iStackFrames, string NotLikeFunction, int StackFrameNumber)
	{
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		NotLikeFunction = NotLikeFunction.ToUpper();
		for (int i = StackFrameNumber; i < iStackFrames.Count; i++)
		{
			scriptStackFrameClass = iStackFrames[i];
			if (!Globals.g_Debugger.GetSymbolFromAddress(scriptStackFrameClass.ReturnAddress).ToUpper().StartsWith(NotLikeFunction))
			{
				return scriptStackFrameClass.ReturnAddress;
			}
		}
		return 0.0;
	}

	[DebuggerStepThrough]
	public IDbgCritSec OwnsCritSec(double threadId)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		dynamic val = null;
		foreach (IDbgCritSec critSec in Globals.g_Debugger.CritSecs)
		{
			IDbgCritSec val2 = critSec;
			if ((double)val2.OwnerThreadID == threadId)
			{
				val = val2;
				break;
			}
		}
		return (IDbgCritSec)val;
	}

	[DebuggerStepThrough]
	public bool IsIISIntrinsicsStack(CacheFunctions.ScriptThreadClass ExceptionThread, out bool bIsUnmarshaling)
	{
		bool flag = false;
		flag = false;
		bIsUnmarshaling = false;
		if (ExceptionThread.FindFrameInStack(Globals.g_COMRuntimeModule + "!COMARSHALINTERFACE") > -1)
		{
			if (ExceptionThread.FindFrameInStack("COMSVCS!VARIANTMARSHAL") > -1 && ExceptionThread.FindFrameInStack("COMSVCS!CASSOCIATION::MARSHALINTERFACE") > -1)
			{
				flag = true;
				bIsUnmarshaling = false;
			}
		}
		else if (ExceptionThread.FindFrameInStack(Globals.g_COMRuntimeModule + "!COUNMARSHALINTERFACE") > -1 && ExceptionThread.FindFrameInStack("COMSVCS!VARIANTUNMARSHAL") > -1 && ExceptionThread.FindFrameInStack("COMSVCS!CASSOCUNMARSHALER::UNMARSHALINTERFACE") > -1)
		{
			flag = true;
			bIsUnmarshaling = true;
		}
		return flag;
	}

	[DebuggerStepThrough]
	public string GetFunctionNameNoUpper(double addr)
	{
		string text;
		if (Globals.g_FunctionNameFromAddrCache.ContainsKey(addr))
		{
			text = Globals.g_FunctionNameFromAddrCache[addr];
		}
		else
		{
			text = CacheFunctions.GetSymbolFromAddress(addr);
			int num = Globals.HelperFunctions.InStrRev(text, "+");
			if (num > 0)
			{
				text = Globals.HelperFunctions.Left(text, num - 1);
			}
			Globals.g_FunctionNameFromAddrCache.Add(addr, text);
		}
		return text;
	}

	[DebuggerStepThrough]
	public bool IsNullOrEmpty(object o)
	{
		if (o is string)
		{
			return string.IsNullOrEmpty((string)o);
		}
		if (o is Array)
		{
			if (o != null)
			{
				return ((Array)o).Length == 0;
			}
			return true;
		}
		return o == null;
	}

	[DebuggerStepThrough]
	public string GetSpecialSTABlurb(string sSTAType)
	{
		string text = null;
		text = "service calls for <b>all</b> instances of <b>all</b> ";
		if (sSTAType != "MAIN")
		{
			text += "\"unconfigured\" (non-COM+) ";
		}
		text += "components with ThreadingModel = ";
		switch (sSTAType)
		{
		case "AT":
			text += "'Apartment'";
			break;
		case "STMT":
		case "MAIN":
			text += "'Single' or ''";
			break;
		}
		if (sSTAType == "STMT" || sSTAType == "AT")
		{
			text += " which are instantiated from MTA threads in the process";
		}
		switch (sSTAType)
		{
		case "AT":
		case "STMT":
			text = text + " (the \"" + sSTAType.ToString() + " host STA\")";
			break;
		case "MAIN":
			text += " (the \"main STA\")";
			break;
		}
		return text;
	}
}
