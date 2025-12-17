using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using CrashHangExtLib;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.Reports;
using IISInfoLib;
using MemoryExtLib;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.AnalysisRules;

internal class AnalyzeCrash
{
	public static void AnalyzeExceptionThread(CacheFunctions.ScriptThreadClass Thread)
	{
		ReportSection currentSection = Globals.Manager.CurrentSection;
		int num = 0;
		Globals.HelperFunctions.ResetStatusNoIncrement("Dumping Thread Data");
		if (Globals.g_Debugger.IsCrashDump)
		{
			_ = Thread.StackFrames;
			Globals.g_Debugger.Execute(".ecxr");
			Thread.FlushStackFrames();
		}
		if (!Globals.Manager.DoHangAnalysisOnCrashDumps)
		{
			ReportSection val = currentSection.AddChildSection("FaultingThread", (SectionType)0);
			val.Title = "Faulting Thread";
			val.Write("<p class='myCustomText'>");
			if (Globals.g_ExtendedThreadInfoAvailable)
			{
				val.Write("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
				if (Thread.StartAddress != 0.0)
				{
					val.Write("<tr><td>Entry point</td><td>&nbsp;&nbsp;<b>" + Globals.g_Debugger.GetSymbolFromAddress(Thread.StartAddress) + "</b></td></tr>");
				}
				val.Write(string.Concat("<tr><td>Create time</td><td>&nbsp;&nbsp;<b>", Thread.CreateTime, "</b></td></tr>"));
				Thread.GetUserTime(out var Days, out var Hours, out var Minutes, out var Seconds, out var MilliSeconds);
				val.Write("<tr><td>Time spent in user mode</td><td>&nbsp;&nbsp;<b>" + Days + " Days " + Hours + ":" + Minutes + ":" + Seconds + "." + MilliSeconds + "</b></td></tr>");
				Thread.GetKernelTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
				val.Write("<tr><td>Time spent in kernel mode</td><td>&nbsp;&nbsp;<b>" + Days + " Days " + Hours + ":" + Minutes + ":" + Seconds + "." + MilliSeconds + "</b></td></tr>");
				val.Write("</table><br><br>");
			}
			val.Write("</p>");
			Globals.AnalyzeManaged.LoadCLRInformation();
			num += 40;
			if (Globals.AnalyzeManaged.IsClrExtensionExecuting() && !string.IsNullOrWhiteSpace(Thread.ClrStackReportNoArgs))
			{
				val.Write("<BR>");
				val.Write("<b>.NET Call Stack</b><br><br>");
				val.Write(Thread.ClrStackReportNoArgs);
				val.Write("<BR>");
				val.Write("<b>Full Call Stack</b><br><br>");
			}
			val.Write(Thread.StackReportWithArgs);
			val.Write("<BR>");
			Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing Exception Info");
			IClientConnection clientConnectionByThreadID = Globals.g_HTTPInfo.GetClientConnectionByThreadID(Thread.ThreadID);
			if (clientConnectionByThreadID != null)
			{
				val = currentSection.AddChildSection("CLIENTCONN", (SectionType)0);
				val.Title = "Client connection executing on thread" + Thread.ThreadID;
				Globals.Manager.CurrentSection = val;
				Globals.ReportHTTPInfo.ReportClientConnection(clientConnectionByThreadID);
				Globals.Manager.CurrentSection = currentSection;
			}
			IASPRequest aSPRequestByThreadID = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
			if (aSPRequestByThreadID != null)
			{
				val = currentSection.AddChildSection("ASPREQ" + Thread.ThreadID, (SectionType)0);
				val.Title = "ASP request executing on thread" + Thread.ThreadID;
				Globals.Manager.CurrentSection = val;
				Globals.ReportASPInfo.ReportASPRequest(aSPRequestByThreadID);
				Globals.ReportASPInfo.OutputVBScriptStack(aSPRequestByThreadID);
				Globals.Manager.CurrentSection = currentSection;
			}
		}
		ReportSection val2 = currentSection.AddChildSection("ExceptionInformation", (SectionType)0);
		val2.Title = "Exception Information";
		val2.IncludeInTOC = false;
		Globals.Manager.CurrentSection = val2;
		AnalyzeException(Globals.g_Debugger.NativeException, Thread, suppressSummary: false);
		Globals.Manager.CurrentSection = currentSection;
	}

	public static void AnalyzeException(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		string text = "";
		NetDbgException val = ExceptionObj;
		if (ExceptionObj.NestedExceptionAddress == 0.0)
		{
			text = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode);
		}
		else
		{
			do
			{
				ExceptionObj = Globals.g_Debugger.GetExceptionObjectFromAddress(ExceptionObj.NestedExceptionAddress);
				if (ExceptionObj != null)
				{
					text = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode);
					continue;
				}
				text = Globals.g_Debugger.GetAs32BitHexString(val.ExceptionCode);
				ExceptionObj = val;
				break;
			}
			while (ExceptionObj.NestedExceptionAddress != 0.0);
		}
		switch (text)
		{
		case "0xc0000005":
		case "0xc00000fd":
		case "0xc0000374":
			AnalyzeAV(ExceptionObj, ExceptionThread, suppressSummary);
			break;
		case "0x80000003":
			AnalyzeBP(ExceptionObj, ExceptionThread, suppressSummary);
			break;
		case "0xc000008f":
			AnalyzeVB(ExceptionObj, ExceptionThread, suppressSummary);
			break;
		case "0x800706be":
		case "0x6be":
		case "0x800706bf":
		case "0x6bf":
		case "0x800706ba":
		case "0x6ba":
			Analyze6Bx(ExceptionObj, ExceptionThread, suppressSummary);
			break;
		case "0xe0434f4d":
		case "0xe0434352":
			if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				AnalyzeClr(ExceptionObj, ExceptionThread, suppressSummary);
			}
			break;
		default:
			AnalyzeUnknown(ExceptionObj, ExceptionThread, suppressSummary);
			break;
		}
	}

	private static bool IsMicrosoftISAPI(double Address)
	{
		IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(Address);
		bool result = false;
		if (moduleByAddress != null)
		{
			switch (moduleByAddress.ModuleName.ToUpper())
			{
			case "MD5FILT":
			case "SSPIFILT":
			case "COMPFILT":
			case "FPEXEDLL":
				result = true;
				break;
			}
		}
		return result;
	}

	public static void Analyze6Bx(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		string text = "";
		if (Globals.HelperFunctions.IsIISIntrinsicsStack(ExceptionThread, out var bIsUnmarshaling))
		{
			if (bIsUnmarshaling)
			{
				text = "un";
			}
			GetRPCErrorInfo(ExceptionObj, out var sShortErr, out var sErrDesc);
			string text2 = "In " + Globals.g_ShortDumpFileName + ", error <b><Font Color='Red'>" + sShortErr + "</b></Font>";
			if (sErrDesc.GetSafeLength() > 0)
			{
				text2 = text2 + " (\"" + sErrDesc + "\")";
			}
			text2 = text2 + " occurred while " + text + "marshaling the <b>IIS Intrinsic objects<b>.";
			string text3 = ((text.GetSafeLength() != 0) ? "Unm" : "M");
			string text4 = text3 + "arshaling of the IIS intrinsic objects across machines commonly fails due to network transport failures. Possible solutions to this problem include:<ul><li>Overcome intermittent network problems by improving the application code to retry the failing  operation. Note this is a general best-practice for all DCOM/RPC applications.</li><li>Eliminate the potential for this type of failure by disabling the IISIntrinsics property on all COM+  components which do ! require their use.  Note this is a best-practice since it reduces unnecessary application processing and network overhead.</li><li>Collect a network capture of the problem to investigate the network transport failure.</li></ul>See the following Knowledge Base article for more information: <a target='_blank' href='http://support.microsoft.com/?id=287422'> IISIntrinsics flow by default when you COM+ components from IIS 5.0 and later applications</a>";
			Globals.Manager.Write(text2);
			if (!suppressSummary)
			{
				Globals.Manager.ReportError(text2, text4, 1000, "{e6c59ce1-f1da-4adb-9a84-baa96056f7d0}");
			}
			else
			{
				AnalyzeUnknown(ExceptionObj, ExceptionThread, suppressSummary);
			}
		}
	}

	public static void GetRPCErrorInfo(NetDbgException ExceptionObj, out string sShortErr, out string sErrDesc)
	{
		string text = "";
		sShortErr = "";
		sErrDesc = "";
		text = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode);
		switch (text)
		{
		case "6be":
		case "0x6be":
		case "800706be":
		case "0x800706be":
			sShortErr = "6be";
			sErrDesc = "The remote procedure failed";
			break;
		case "6bf":
		case "0x6bf":
		case "800706bf":
		case "0x800706bf":
			sShortErr = "6bf";
			sErrDesc = "The remote procedure failed and did ! execute";
			break;
		case "6ba":
		case "0x6ba":
		case "800706ba":
		case "0x800706ba":
			sShortErr = "6ba";
			sErrDesc = "The RPC server is unavailable";
			break;
		default:
			sShortErr = text;
			break;
		}
	}

	public static void AnalyzeClr(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		double num = 0.0;
		IDbgModule val = null;
		string text6 = null;
		string text7 = null;
		bool flag = false;
		text4 = Globals.g_Debugger.Execute("~" + ExceptionThread.ThreadID + " kb 1000").ToUpper();
		string text8;
		if (Globals.g_Debugger.ClrVersionInfo.Version.Major >= 2 && text4.Contains("RAISECROSSCONTEXTEXCEPTION") && text4.Contains("REALCOMPLUSTHROW"))
		{
			text8 = "In " + Globals.g_ShortDumpFileName + " an <font color=red><b>unhandled .net exception</b></font> happened on " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + " and has caused the process to crash. <br><br>When an unhandled exception is thrown in a Microsoft ASP.NET-based application that is built on the Microsoft .NET Framework 2.0, the application unexpectedly quits because the default policy for unhandled exceptions has changed in the .NET Framework 2.0. By default, the policy for unhandled exceptions is to end the worker process. <br>";
			string managedExceptionPtr = Globals.AnalyzeManaged.GetManagedExceptionPtr(ExceptionThread);
			string text9 = Globals.AnalyzeManaged.DumpString(managedExceptionPtr, "_className");
			string text10 = Globals.AnalyzeManaged.DumpString(managedExceptionPtr, "_message");
			string text11 = Globals.AnalyzeManaged.GetStackTraceFromException(managedExceptionPtr);
			if (text11 == "Stack Information not available")
			{
				text11 = "";
			}
			string aSrcStr = Globals.AnalyzeManaged.DumpString(managedExceptionPtr, "_remoteStackTraceString");
			aSrcStr = Globals.AnalyzeManaged.regexReplace(aSrcStr, "\n", "<br>");
			text8 += "<br/><b><u>Exception Details</u></b><br/>";
			text8 = text8 + text9 + "<br>";
			text8 = text8 + text10 + "<br>";
			text8 = text8 + text11 + "<br>";
			text8 = text8 + aSrcStr + "<br>";
			string RecommendationString = null;
			string AdditionalDescriptionString = null;
			string SolutionSourceID = null;
			if (Globals.AnalyzeManaged.IsWellKnownUnhandledException(text9, text10, text11, aSrcStr, ExceptionThread, ref AdditionalDescriptionString, ref RecommendationString, ref SolutionSourceID))
			{
				text = RecommendationString;
				text8 = text8 + "<BR><BR>" + AdditionalDescriptionString;
				text5 = SolutionSourceID;
			}
			else
			{
				text = "Review the article <a href='http://support.microsoft.com/?id=911816'>911816</a> to read more about the .net unhandled exception policy or check out the post <a href='http://blogs.msdn.com/tess/archive/2006/04/27/584927.aspx'>ASP.NET 2.0 Crash case study: Unhandled exceptions</a>";
				text = text + "<br/><br/><br/> To view more details about the exception that was unhandled review the <a href='#ManagedExceptionsReport" + Globals.g_UniqueReference + "'>.NET Exceptions Report</a>";
			}
			Globals.Manager.ReportError(text8, text, 0, "{89d8fe19-1789-4bfd-9da2-1ae79bc26d8f}");
			flag = true;
		}
		Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode);
		string text12 = Globals.HelperFunctions.GetFunctionNameNoUpper(ExceptionObj.ExceptionAddress);
		CacheFunctions.ScriptModuleClass moduleFromAddress = CacheFunctions.GetModuleFromAddress(ExceptionObj.ExceptionAddress);
		text8 = "In " + Globals.g_ShortDumpFileName + " the assembly instruction at <b>" + text12;
		if (moduleFromAddress == null)
		{
			text8 += "</b> which does ! correspond to any known native module in the process";
			text = "Please contact Microsoft Corporation for troubleshooting steps on stack corruption<br>";
		}
		else
		{
			text8 = text8 + "</b> in <b>" + moduleFromAddress.ImageName;
			if (moduleFromAddress.VSCompanyName != "")
			{
				text8 = text8 + "</b> from <b>" + moduleFromAddress.VSCompanyName;
			}
			string text13 = ((!text12.ToUpper().StartsWith("KERNEL32!RAISEEXCEPTION")) ? "KernelBase!RaiseException" : "Kernel32!RaiseException");
			if (text13 != "")
			{
				num = Globals.HelperFunctions.GetDirectCaller(ExceptionThread.StackFrames, Globals.AnalyzeManaged.GetClrModule().ModuleName + "!", 1);
				moduleFromAddress = CacheFunctions.GetModuleFromAddress(num);
				if (num > 0.0)
				{
					text = "Review the faulting stack for thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + " to determine root cause for the exception.<br>" + Globals.HelperFunctions.GetVendorMessage(num);
					text12 = Globals.HelperFunctions.GetFunctionNameNoUpper(num);
				}
				else
				{
					text = "<br>An exception thrown by <Font Color='Red'><b>" + text13 + "</b></Font> usually indicates a problem with another module. Please review the stack for the faulting thread (" + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + ") further to determine which module actually threw the exception raised by Kernel32.dll.<br><br>";
				}
			}
			else if (text12.ToUpper().StartsWith("NTDLL!KIRAISEUSEREXCEPTIONDISPATCHER"))
			{
				string value = "ADVAPI32!REGCLOSEKEY;KERNEL32!CLOSEHANDLE";
				Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = ExceptionThread.StackFrames;
				for (int i = 0; i < stackFrames.Count; i++)
				{
					CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = stackFrames[i];
					text12 = scriptStackFrameClass.GetFrameText().ToUpper();
					if (text12.ToUpper().StartsWith(value))
					{
						val = Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.ReturnAddress);
						num = scriptStackFrameClass.ReturnAddress;
						text = "This exception occured as a result of an invalid handle passed to " + text12 + " by the following function:<br><br><b>";
						text12 = Globals.g_Debugger.GetSymbolFromAddress(scriptStackFrameClass.ReturnAddress);
						text = text + text12 + "</b><br><br>Please follow up with the vendor of this module";
						if (val.VSCompanyName != "")
						{
							text = text + ", <b>" + val.VSCompanyName + "</b>,";
						}
						text += " for further assistance with this issue.";
						moduleFromAddress = CacheFunctions.GetModuleFromAddress(num);
					}
				}
			}
			else
			{
				text = Globals.HelperFunctions.GetVendorMessage(ExceptionObj.ExceptionAddress);
			}
		}
		text8 = text8 + "</b> has caused a <b><font color='red'>CLR Exception</font></b> on thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID);
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			string managedExceptionPtr2 = Globals.AnalyzeManaged.GetManagedExceptionPtr(ExceptionThread);
			if (managedExceptionPtr2 != "" && managedExceptionPtr2 != "00000000")
			{
				text2 = Globals.AnalyzeManaged.GetManagedExceptionType(managedExceptionPtr2, bInnerException: false);
				if (text2 != "")
				{
					string text14 = "<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><td>&nbsp;&nbsp;&nbsp;&nbsp;Type:&nbsp;&nbsp;</td><td><b><font color='red'>" + HttpUtility.HtmlEncode(text2) + "</font></b></td></tr>";
					text3 = Globals.AnalyzeManaged.GetManagedExceptionMsg(managedExceptionPtr2, bInnerException: false);
					if (!string.IsNullOrEmpty(text3))
					{
						text3.Replace(Globals.vbcrlf, "<br>");
						text3.Replace('\n'.ToString(), "<br>");
						text14 = text14 + "<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;Message:&nbsp;&nbsp;</td><td><font color='red'>" + HttpUtility.HtmlEncode(text3) + "</font></td></tr>";
					}
					text14 += "</table>";
					text8 = text8 + " with the following error information:<br>" + text14;
				}
				text6 = GetInnerExceptionDetailSection(managedExceptionPtr2);
				if (text6 != null)
				{
					text7 = "<br/><br/>This exception contains <a href='#InnerExceptions_" + Globals.g_UniqueReference + "_" + managedExceptionPtr2 + "'>Inner Exceptions</a>";
				}
			}
		}
		if (num > 0.0)
		{
			text8 = text8 + "<br>This exception originated from <b><font color='darkblue'>" + text12 + "</font></b>. ";
		}
		if (!string.IsNullOrEmpty(text7))
		{
			text8 += text7;
		}
		Globals.Manager.Write(text8);
		if (!string.IsNullOrEmpty(text6))
		{
			Globals.Manager.Write(text6);
		}
		if (!(text5 == ""))
		{
			return;
		}
		text5 = "{41afe6d5-6758-40ba-b76c-969163b632f3}";
		if (!suppressSummary && !flag)
		{
			Globals.Manager.ReportError(text8, text, 1000, text5);
			if (moduleFromAddress != null && Globals.g_Debugger.IsCrashDump)
			{
				Globals.Manager.Write("<BR><BR>");
				Globals.HelperFunctions.ModuleInfo(moduleFromAddress);
			}
		}
	}

	public static string GetInnerExceptionDetailSection(string exceptionAddress)
	{
		ulong num = (ulong)Globals.HelperFunctions.FromHex(exceptionAddress);
		ClrException exceptionObject = Globals.g_Debugger.ClrHeap.GetExceptionObject(num);
		exceptionObject = exceptionObject.Inner;
		if (exceptionObject == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendFormat("<br/><br/><a id='InnerExceptions_{0}_{1}'></a>", Globals.g_UniqueReference, exceptionAddress);
		stringBuilder.AppendLine();
		int num2 = 1;
		while (exceptionObject != null)
		{
			stringBuilder.AppendFormat("Exception {0} (0x{1})<br/>", num2, Convert.ToString((long)exceptionObject.Address, 16));
			stringBuilder.AppendFormat("Type: <b><font color='red'>{0}</font></b><br/>", Globals.AnalyzeManaged.HTMLEncode(exceptionObject.Type.Name));
			stringBuilder.AppendFormat("Exception Message: <font color='red'>{0}</font><br/>", Globals.AnalyzeManaged.HTMLEncode(exceptionObject.Message));
			stringBuilder.Append("StackTrace:");
			string stackTraceFromException = Globals.AnalyzeManaged.GetStackTraceFromException(Convert.ToString((long)exceptionObject.Address, 16));
			if (string.IsNullOrEmpty(stackTraceFromException))
			{
				stringBuilder.Append(" [not available]");
			}
			else
			{
				stringBuilder.AppendFormat("<br/><pre><code><font color='darkblue'>{0}</font></code></pre>", Globals.AnalyzeManaged.HTMLEncode(stackTraceFromException));
			}
			num2++;
			exceptionObject = exceptionObject.Inner;
			if (exceptionObject != null)
			{
				stringBuilder.Append("<br/><br/>");
			}
		}
		return stringBuilder.ToString();
	}

	public static void AnalyzeVB(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass2 = null;
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		string text6 = null;
		string text7 = null;
		double num = 0.0;
		double num2 = 0.0;
		bool flag = false;
		text7 = "";
		text = "";
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = ExceptionThread.StackFrames;
		if (stackFrames.Count >= 2)
		{
			scriptModuleClass = CacheFunctions.GetModuleFromAddress(stackFrames[1].InstructionAddress);
			if (scriptModuleClass != null)
			{
				string text8 = scriptModuleClass.ModuleName.ToLower();
				if (!(text8 == "msvbvm50"))
				{
					if (!(text8 == "msvbvm60"))
					{
						AnalyzeUnknown(ExceptionObj, ExceptionThread, suppressSummary);
						return;
					}
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
		}
		if (stackFrames.Count >= 3)
		{
			for (int i = 2; i <= stackFrames.Count - 1; i++)
			{
				scriptModuleClass2 = CacheFunctions.GetModuleFromAddress(stackFrames[i].InstructionAddress);
				if (scriptModuleClass2 != null && scriptModuleClass2.ModuleName.ToLower() != scriptModuleClass.ModuleName.ToLower())
				{
					text = stackFrames[i].GetFrameText(includeOffset: true, Globals.Manager.SourceInfoEnabled);
					break;
				}
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			scriptModuleClass2 = null;
		}
		if (flag)
		{
			text4 = "<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>";
			num = ExceptionThread.Register("ebx");
			num2 = Globals.g_Debugger.ReadDWord(num + 24.0);
			if (num2 != 0.0 && Globals.g_Debugger.GetSymbolFromAddress(num2).ToLower().StartsWith("msvbvm60!"))
			{
				text7 = Globals.g_Debugger.GetAs32BitHexString(Globals.g_Debugger.ReadDWord(num + 28.0));
				text6 = Globals.g_Debugger.ReadUnicodeString(Globals.g_Debugger.ReadDWord(num + 8.0));
				text5 = Globals.g_Debugger.ReadUnicodeString(Globals.g_Debugger.ReadDWord(num + 4.0));
				text4 += GetVBErrorNumTableRow(text7);
				text4 = text4 + "<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;Description:</td><td>&nbsp;&nbsp;" + text6 + "</td></tr>";
				text4 = text4 + "<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;Source:</td><td>&nbsp;&nbsp;" + text5 + "</td></tr>";
			}
			if (string.IsNullOrEmpty(text7))
			{
				text4 += GetVBErrorNumTableRow(Globals.HelperFunctions.GetArgAsHexString(ExceptionThread.StackFrames[1], 1));
			}
			text4 += "</table>";
		}
		if (string.IsNullOrEmpty(text))
		{
			text2 = "In " + Globals.g_ShortDumpFileName + " an undertermined source";
			text3 = "<br>A <Font Color='Red'><b>Visual Basic Runtime exception</b></Font> usually indicates a problem with a custom module written in Visual Basic. Please review the stack for the faulting thread (" + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + ") further to determine which module actually caused the exception.";
			text3 += "<br><br><br><font color='Gray'>Note: unresolved symbols may have prevented DebugDiag from determining the source of this exception.</font>";
		}
		else
		{
			text2 = "In " + Globals.g_ShortDumpFileName + " the assembly instruction at <b>" + text;
			text3 = "Please follow up with the vendor <b>";
			text2 = text2 + "</b> in <b>" + scriptModuleClass2.ImageName;
			if (!string.IsNullOrEmpty(scriptModuleClass2.VSCompanyName))
			{
				text3 += scriptModuleClass2.VSCompanyName;
				text2 = text2 + "</b> from <b>" + scriptModuleClass2.VSCompanyName;
			}
			text3 = text3 + "</b> for <b>" + scriptModuleClass2.ImageName + "</b><br>";
		}
		text2 = text2 + "</b> has caused a <font color='red'><b>Visual Basic Runtime exception</b></font> (0xc000008f) on thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + " with the following error information:<br>" + text4;
		Globals.Manager.Write(text2);
		if (!suppressSummary)
		{
			Globals.Manager.ReportError(text2, text3, 1000, "{5246d10b-1eee-4d53-9f2c-f8d10d6d45f0}");
		}
		if (scriptModuleClass2 != null && Globals.g_Debugger.IsCrashDump)
		{
			Globals.Manager.Write("<BR><BR>");
			Globals.HelperFunctions.ModuleInfo(scriptModuleClass2);
		}
	}

	private static string GetVBErrorNumTableRow(string HexErrorNumStr)
	{
		if (HexErrorNumStr.Substring(0, 2).ToLower() != "0x")
		{
			HexErrorNumStr = "0x" + HexErrorNumStr;
		}
		if (HexErrorNumStr.Substring(0, 6).ToLower() == "0x800a")
		{
			return "<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;VB Error #:</td><td>&nbsp;&nbsp;" + Convert.ToInt32("0x" + HexErrorNumStr.Substring(7, HexErrorNumStr.GetSafeLength() - 7), 16) + "</td></tr>";
		}
		return "<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;Error code:</td><td>&nbsp;&nbsp;" + HexErrorNumStr + "</td></tr>";
	}

	public static void AnalyzeAV(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		//IL_0b96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9c: Expected O, but got Unknown
		//IL_0c22: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c28: Expected O, but got Unknown
		IDbgModule val = null;
		IDbgModule val2 = null;
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		string text6 = null;
		string text7 = "";
		string text8 = null;
		string text9 = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		bool flag = false;
		int num = 0;
		INTHeap iNTHeap = null;
		string text10 = null;
		Hashtable hashtable = new Hashtable();
		text3 = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode).ToLower();
		text4 = ((text3 == "0xc00000fd") ? "a <b>stack overflow exception (0xC00000FD)</b>" : ((!(text3 == "0xc0000374")) ? "an <b>access violation exception (0xC0000005)</b>" : "a <b>corrupted heap exception (0xC0000374)</b>"));
		if (SMTPBugCheck(ExceptionThread, suppressSummary))
		{
			return;
		}
		text2 = Globals.g_Debugger.GetSymbolFromAddress(ExceptionObj.ExceptionAddress);
		val = Globals.g_Debugger.GetModuleByAddress(ExceptionObj.ExceptionAddress);
		string[] array = Globals.HelperFunctions.CheckSymbolType(ExceptionObj.ExceptionAddress).Split(':');
		text7 = (int)ExceptionObj.GetExceptionParam(0) switch
		{
			0 => "<b>read from</b>", 
			1 => "<b>write to</b>", 
			8 => "<b>execute instructions from a non-executable address at</b>", 
			_ => "<b>perform an unknown operation on</b>", 
		};
		text8 = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.GetExceptionParam(1));
		text5 = "In " + Globals.g_ShortDumpFileName + " the assembly instruction at <b>" + text2;
		if (array[2] == "0" && !(array[0] == "UNKNOWN_MODULE"))
		{
			text5 = "<b>WARNING</b> - DebugDiag was not able to locate debug symbols for <b>" + array[0] + "</b>, so the information below may be incomplete.<br><br>" + text5;
		}
		if (val == null)
		{
			Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = ExceptionThread.StackFrames;
			scriptStackFrameClass = stackFrames[0];
			val2 = Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.ReturnAddress);
			if (array[1] == "1")
			{
				string[] array2 = Globals.HelperFunctions.Split(array[0], "_", -1);
				for (int i = 0; i <= stackFrames.Count - 1; i++)
				{
					scriptStackFrameClass = stackFrames[i];
					if (scriptStackFrameClass.GetFrameText().ToUpper().Contains(array2[1]))
					{
						val2 = Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.ReturnAddress);
						if (val2 != null)
						{
							flag = true;
							break;
						}
					}
				}
				text5 = "<br>In <b>" + Globals.g_ShortDumpFileName + "</b> " + text4 + " occured on thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + " when ";
				text5 = ((!flag) ? (text5 + "another Module") : (text5 + "<b>" + val2.LoadedImageName + "</b>"));
				text5 = text5 + " attempted to call the following <b>unloaded</b> Module: <b>" + array2[1] + "</b>.<br><br>";
				text6 = "<br>Please follow up with the vendor of <b>" + array2[1] + "</b> for further assistance.<br><br>";
				Globals.Manager.Write(text5);
				if (array[2] == "0")
				{
					text5 = "<b>WARNING</b> - DebugDiag was not able to locate debug symbols for <b>" + array2[1] + "</b>, so the information below may be incomplete.<br><br>" + text5;
				}
				if (!suppressSummary)
				{
					Globals.Manager.ReportError(text5, text6, 1000, "{3700f3d3-0d00-4451-96bf-299d3b4697fa}");
				}
				return;
			}
			if (val2 != null)
			{
				text = Globals.g_Debugger.GetSymbolFromAddress(scriptStackFrameClass.ReturnAddress).ToUpper();
				val = (text.Contains("NTDLL!EXECUTEHANDLER") ? AnalyzeExceptionLoop(ExceptionThread) : ((!text2.StartsWith("KERNEL32!__SEH_PROLOG") || Globals.g_OSVER < Globals.OS_VER_WIN2K3SP) ? Globals.g_Debugger.GetModuleByAddress(val2.Base) : AnalyzeExceptionLoop(ExceptionThread)));
				if (val == null)
				{
					text6 = StackFunctions.AnalyzeCorruptStack(ExceptionThread);
					text5 += "</b> which does not correspond to any known native Module in the process";
					text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc38}";
				}
				else
				{
					array = Globals.HelperFunctions.CheckSymbolType(val.Base).Split(':');
					text5 = "";
					if (array[2] == "0")
					{
						text5 = "<b>WARNING</b> - DebugDiag was not able to locate debug symbols for <b>" + val.ImageName + "</b>, so the information below may be incomplete.<br><br>";
					}
					text5 = text5 + "In <b>" + Globals.g_ShortDumpFileName + "</b> the Module <b>" + val.ImageName + "</b>";
					if (text == "W3ISAPI!PROCESSISAPICOMPLETION+D4")
					{
						text6 = "This exception was likely caused by a known issue in ASP.DLL. Please see the following knowledge base article for information on how to obtain the hotfix for this issue:<br><br> <a target='_blank' href='http://support.microsoft.com/?id=828869'> 828869 The IIS Worker Process Recycles When You Use the Server.Execute Method </a><br><br>";
						text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc39}";
					}
					else
					{
						text6 = "Please follow up with the vendor of this Module";
						if (!string.IsNullOrEmpty(val.VSCompanyName))
						{
							text6 = text6 + ", <b>" + val.VSCompanyName + "</b>,";
						}
						text6 += " for further assistance with this issue.";
					}
				}
			}
			else
			{
				text6 = StackFunctions.AnalyzeCorruptStack(ExceptionThread);
				text5 += "</b> which does not correspond to any known native Module in the process";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc38}";
			}
		}
		else
		{
			text9 = Globals.g_Debugger.GetSymbolFromAddress(ExceptionObj.ExceptionAddress).ToUpper();
			Globals.Manager.Write(text9);
			switch (text9)
			{
			case "W3SVC!HTTP_REQ_BASE::ONCOMPLETEREQUEST+19E":
				text6 = "<b>The crash identified is a known issue.</b> Please follow the steps in the following Knowledge Base article: <br> <a target='_blank' href='http://support.microsoft.com/?id=873401'> 873401 IIS may not respond after you install MS04-021 on an IIS 4.0 server </a>";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3a}";
				break;
			case "ABOCOMP!IPSTRTODWORD+D7":
				text6 = "<b>The crash identified is a known issue.</b> <br/> <br/> The crash happens if there is an invalid IP address entry inside the <b>%WINDIR%\\system32\\inetsrv\\config\\applicationHost.config file</b>. <br/><br/> Verify all the <b><font color='red'>ipAddress</font></b> entries in the applicationHost.config file to confirm all the ip-address contain 4-tuples.";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3a}";
				break;
			case "ASP!_ALLOCA_PROBE+17":
			case "ASP!_CHKSTK+17":
				text6 = "<br><b>The exception identified is a known issue. <b>Stack Overflow </b> due to huge ASP response buffer.</b> Please follow the steps in the following Knowledge Base article: <br> <a target='_blank' href='http://support.microsoft.com/?id=823818'> 823818 FIX: Memory usage increases and IIS 5.0 stops responding when ASP </a><br><br>";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3b}";
				break;
			case "NTDLL!WCSLEN+9":
			case "MSVCRT!WCSLEN+4":
				val = AnalyzeStringAV(ExceptionThread);
				text6 = "This issue may have been caused by the following dll passing incorrect data to a string function:<br><br><b>" + val.ImageName + "</b>";
				if (!string.IsNullOrEmpty(val.VSCompanyName))
				{
					text6 = text6 + "<br><br>Please follow up with the following vendor regarding this issue:<br><br><b>" + val.VSCompanyName + "</b>";
				}
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3c}";
				break;
			case "NTDLL!RTLDISPATCHEXCEPTION+6":
			case "NTDLL!RTLLOOKUPFUNCTIONTABLE+A":
			case "NTDLL!RTLRAISEEXCEPTION+2B":
				val = AnalyzeExceptionLoop(ExceptionThread);
				text5 = "In <b>" + Globals.g_ShortDumpFileName + "</b> the Module <b>" + val.ImageName + "</b>";
				text6 = "<br>Please follow up with the vendor of this Module";
				if (!string.IsNullOrEmpty(val.VSCompanyName))
				{
					text6 = text6 + ", <b>" + val.VSCompanyName + "</b>,";
				}
				text6 += " for further assistance with this issue.<br><br>";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3d}";
				break;
			case "KERNEL32!__SEH_PROLOG+1A":
				if (Globals.g_OSVER >= Globals.OS_VER_WIN2K3SP)
				{
					val = AnalyzeExceptionLoop(ExceptionThread);
					text5 = "In <b>" + Globals.g_ShortDumpFileName + "</b> the Module <b>" + val.ImageName + "</b>";
					text6 = "<br>Please follow up with the vendor of this Module";
					if (!string.IsNullOrEmpty(val.VSCompanyName))
					{
						text6 = text6 + ", <b>" + val.VSCompanyName + "</b>,";
					}
					text6 += " for further assistance with this issue.<br><br>";
				}
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3e}";
				break;
			case "COMSVCS!CLINKABLE::INSERTBEFORE+13":
				text6 = "<br>The exception identified is a known issue with COM+. Please follow the steps in the following Knowledge Base article: <br><br><a target='_blank' href='http://support.microsoft.com/?id=828743'> 828743 FIX: You receive an access violation when you try to queue work on a thread that has exited</a><br><br>";
				text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc3f}";
				break;
			default:
				if (val != null)
				{
					text6 = "Please follow up with the vendor <b>";
					text5 = text5 + "</b> in <b>" + val.ImageName;
					if (!string.IsNullOrEmpty(val.VSCompanyName))
					{
						text6 += val.VSCompanyName;
						text5 = text5 + "</b> from <b>" + val.VSCompanyName;
					}
					text6 = text6 + "</b> for <b>" + val.ImageName + "</b><br>";
				}
				break;
			}
		}
		text5 = text5 + "</b> has caused " + text4 + " when trying to " + text7 + "</b> memory location <b>" + text8 + "</b> on thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + "<br>";
		if (Globals.g_Debugger.DumpType != "MINIDUMP" && Globals.HeapFunctions.IsHeapFunction(ExceptionObj.ExceptionAddress) && Globals.g_GlobalFlagsValue == 0)
		{
			text6 = "An exception thrown by a heap memory manager function indicates <b>heap corruption</b>. Please click the 'PageHeap Flags...' button in the DebugDiag crash rule configuration dialog to enable PageHeap for the target process  and collect another dump.  For more information, review the following documents: <br><a target='_blank' href='https://msdn.microsoft.com/en-us/library/ff420662.aspx#code-snippet-4'>How to Use the Debug Diagnostic Tool v1.1 (DebugDiag) to Debug User Mode Processes</a><br><a target='_blank' href='http://blogs.msdn.com/b/lagdas/archive/2008/06/24/debugging-heap-corruption-with-application-verifier-and-debugdiag.aspx'>Debugging Heap corruption with Application Verifier and Debugdiag</a><br>";
			if (Globals.g_Debugger.DumpType != "MINIDUMP")
			{
				num = ((IUtils)Globals.g_UtilExt).get_SuspectHeapIndex((uint)ExceptionThread.ThreadID);
				iNTHeap = ((!(Globals.g_Debugger.DumpType != "MINIDUMP") || num == 0) ? null : Globals.g_HeapInfo[num - 1]);
			}
			if (iNTHeap != null)
			{
				text5 = text5 + "<div class='summaryItemCallout'>Heap corruption was detected in heap <b><a href = '" + Globals.HeapFunctions.GetHeapLink(iNTHeap) + "'>" + Globals.HelperFunctions.GetAsHexString(iNTHeap.Handle) + "</a></b></div>However pageheap was <b>not</b> enabled in this dump. Please follow the instructions in the recommendation section for troubleshooting heap corruption issues.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.HelperFunctions.Hex(Globals.g_GlobalFlagsValue) + "</b>";
				ReportSection currentSection = Globals.Manager.CurrentSection;
				ReportSection val3 = currentSection.AddChildSection("DetailedInfo", (SectionType)0);
				val3.Title = "Detailed Info For Corrupt Heap";
				val3.IncludeInTOC = false;
				Globals.Manager.CurrentSection = val3;
				Globals.HeapFunctions.PrintHeapInfo(iNTHeap);
				Globals.Manager.CurrentSection = currentSection;
			}
			else
			{
				text5 = text5 + "<div class='summaryItemCallout'>Heap corruption was detected in " + Globals.g_ShortDumpFileName + "</div>However pageheap was <b>not</b> enabled in this dump. Please follow the instructions in the recommendation section for troubleshooting heap corruption issues.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.HelperFunctions.Hex(Globals.g_GlobalFlagsValue) + "</b>";
			}
		}
		else if (Globals.HeapFunctions.IsHeapFunction(ExceptionObj.ExceptionAddress) && Globals.g_GlobalFlagsValue != 0)
		{
			val = Globals.g_Debugger.GetModuleByAddress(Globals.HeapFunctions.AnalyzeHeapCorruption(ExceptionThread).Base);
			text5 = text5 + "<div class='summaryItemCallout'>Heap corruption was detected in this dump</div>Please follow up with the vendor of the Module listed in the recommendation section for further assistance on this issue.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.HelperFunctions.Hex(Globals.g_GlobalFlagsValue) + " (" + Globals.HeapFunctions.GetGlobalFlagDescription(Globals.g_GlobalFlagsValue) + ")</b>";
			text6 = "The heap corruption detected in this dump was likely caused by the following Module:<br><br><b>" + val.ImageName + "</b>";
			text6 = (string.IsNullOrEmpty(val.VSCompanyName) ? (text6 + "<br><br>Please follow up with the vendor of this Module for further assitance.") : (text6 + "<br><br>Please follow up with the following vendor regarding this issue:<br><br><b>" + val.VSCompanyName + "</b>"));
		}
		else if (IsMicrosoftISAPI(ExceptionObj.ExceptionAddress) && Globals.g_Debugger.DumpType != "MINIDUMP")
		{
			foreach (IDbgModule module in Globals.g_Debugger.Modules)
			{
				val = module;
				if (val.VSCompanyName.ToUpper().Contains("MICROSOFT") && val.IsISAPIFilter)
				{
					hashtable.Add(val.Base, val);
				}
			}
			if (hashtable.Count > 0)
			{
				text6 = "The access violation exception originated from a Microsoft ISAPI filter. Usually this indicates ISAPI filter context corruption caused by another ISAPI filter loaded in the process. The following Modules were identified as non-Microsoft ISAPI filters  loaded in this process.<br><br>";
				foreach (DictionaryEntry item in hashtable)
				{
					val = (IDbgModule)item.Value;
					text6 = text6 + "<b>" + val.ImageName + "</b>";
					if (!string.IsNullOrEmpty(val.VSCompanyName))
					{
						text6 = text6 + " from <b>" + val.VSCompanyName + "</b>";
					}
					text6 += "<br>";
				}
			}
		}
		if (text3 == "0xc00000fd")
		{
			int num2 = 0;
			num2 = Globals.AnalyzeComPlus.CountAppInvokeFrames(ExceptionThread, 50);
			if (num2 > 1)
			{
				text6 = ((num2 != 50) ? (text6 + "<br><br>Note " + num2 + " stacked COM calls") : (text6 + "<br><br>Note at least 50 stacked COM calls"));
				text6 += " were detected on this thread.  Too many COM calls stacking on the same thread simultaneously can exaust the thread's stack space and cause this exception.  Review the call stack for this thread to determine why multiple COM calls are entering the thread simultatneously, and make the necessary changes to the application code in order to prevent this scenario.";
				if (Globals.Manager.DoHangAnalysisOnCrashDumps)
				{
					if (Globals.AnalyzeComPlus.IsWellKnownCOMSTA(ExceptionThread.SystemID))
					{
						text6 = text6 + " This thread is a well-konwn COM STA Thread.  The information in the " + Globals.AnalyzeComPlus.GetCOMSTAReportLink() + " may be useful in determining why multiple COM calls are entering the thread simultatneously.";
					}
				}
				else
				{
					text6 += " If this thread happens to be a well-known COM thread, then DebugDiag hang analysis will provide more details and potential solutions.  To run hang analysis on this crash dump, you can edit the IISAnalysis.asp script (located in the DebugDiag\\Scripts folder) and set the <b>g_DoHangAnalysisOnCrashDumps</b> constant to <font color='Red'><b>True</b></font>.";
				}
			}
		}
		Globals.Manager.Write(text5);
		if (string.IsNullOrEmpty(text10))
		{
			text10 = "{f3f150d0-75f3-45a1-9d0f-bab4c71bdc37}";
		}
		if (!suppressSummary)
		{
			Globals.Manager.ReportError(text5, text6, 1000, text10);
		}
		if (val != null && Globals.g_Debugger.IsCrashDump)
		{
			Globals.Manager.Write("<BR><BR>");
			Globals.HelperFunctions.ModuleInfo(val);
		}
	}

	public static void AnalyzeBP(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		string text = null;
		string text2 = null;
		int num = 0;
		text = Globals.g_Debugger.GetSymbolFromAddress(ExceptionObj.ExceptionAddress);
		IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(ExceptionObj.ExceptionAddress);
		if (Globals.g_Debugger.GetSymbolFromAddress(ExceptionThread.StackFrames[0].ReturnAddress).ToUpper().Contains("NTDLL!DBGUIREMOTEBREAKIN"))
		{
			if (!suppressSummary)
			{
				Globals.Manager.ReportOther("DebugDiag has detected that this crash dump (" + Globals.g_ShortDumpFileName + ") was generated as a result of the attached debugger being shut down manually. Please reattach the debugger to the target process and wait for the problem to reoccur.", "", "Notification", "notificationicon.png", 0, "{b8abdc5a-45f2-48b0-a754-77c675da7497}");
			}
			return;
		}
		string text3 = "In " + Globals.g_ShortDumpFileName + " the assembly instruction at <b>" + text;
		if (moduleByAddress == null)
		{
			text3 += "</b> which does not correspond to any known native Module in the process";
			text2 = "Please contact Microsoft Corporation for troubleshooting steps on stack corruption<br>";
		}
		else
		{
			text2 = "Please follow up with the vendor <b>";
			text3 = text3 + "</b> in <b>" + moduleByAddress.ImageName;
			if (!string.IsNullOrEmpty(moduleByAddress.VSCompanyName))
			{
				text2 += moduleByAddress.VSCompanyName;
				text3 = text3 + "</b> from <b>" + moduleByAddress.VSCompanyName;
			}
			text2 = text2 + "</b> for <b>" + moduleByAddress.ImageName + "</b><br>";
		}
		text3 = text3 + "</b> has caused a <b>breakpoint exception (0x80000003)</b> on thread " + GetFaultingThreadIDWithLink(ExceptionThread.ThreadID) + "<br>";
		if (Globals.HeapFunctions.IsHeapFunction(ExceptionThread.StackFrames[0].ReturnAddress) && Globals.g_GlobalFlagsValue == 0)
		{
			text2 = "A breakpoint exception thrown by a heap memory manager function indicates <b>heap corruption</b>. Please click the 'PageHeap Flags...' button in the DebugDiag crash rule configuration dialog to enable PageHeap for the target process  and collect another dump.  For more information, review the following documents: <br><a target='_blank' href='https://msdn.microsoft.com/en-us/library/ff420662.aspx#code-snippet-4'>How to Use the Debug Diagnostic Tool v1.1 (DebugDiag) to Debug User Mode Processes</a><br><a target='_blank' href='http://blogs.msdn.com/b/lagdas/archive/2008/06/24/debugging-heap-corruption-with-application-verifier-and-debugdiag.aspx'>Debugging Heap corruption with Application Verifier and Debugdiag</a><br>";
			num = ((IUtils)Globals.g_UtilExt).get_SuspectHeapIndex((uint)ExceptionThread.ThreadID);
			INTHeap iNTHeap = ((!(Globals.g_Debugger.DumpType != "MINIDUMP") || num == 0) ? null : Globals.g_HeapInfo[num - 1]);
			if (iNTHeap != null)
			{
				text3 = text3 + "<div class='summaryItemCallout'>Heap corruption was detected in heap <b><a href = '" + Globals.HeapFunctions.GetHeapLink(iNTHeap) + "'>" + Globals.HelperFunctions.GetAsHexString(iNTHeap.Handle) + "</a></b></div>However pageheap was <b>not</b> enabled in this dump. Please follow the instructions in the recommendation section for troubleshooting heap corruption issues.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.HelperFunctions.Hex(Globals.g_GlobalFlagsValue) + "</b>";
				ReportSection currentSection = Globals.Manager.CurrentSection;
				ReportSection val = currentSection.AddChildSection("DetailedInfo", (SectionType)0);
				currentSection.Title = "Detailed Info For Corrupt Heap";
				val.IncludeInTOC = false;
				Globals.Manager.CurrentSection = val;
				Globals.HeapFunctions.PrintHeapInfo(iNTHeap);
				Globals.Manager.CurrentSection = currentSection;
			}
			else
			{
				text3 = text3 + "<div class='summaryItemCallout'>Heap corruption was detected in " + Globals.g_ShortDumpFileName + "</div>However pageheap was <b>not</b> enabled in this dump. Please follow the instructions in the recommendation section for troubleshooting heap corruption issues.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.HelperFunctions.Hex(Globals.g_GlobalFlagsValue) + "</b>";
			}
		}
		else if (Globals.HeapFunctions.IsHeapFunction(ExceptionThread.StackFrames[0].ReturnAddress) && Globals.HeapFunctions.IsPageHeapEnabled(Globals.g_GlobalFlagsValue))
		{
			moduleByAddress = Globals.g_Debugger.GetModuleByAddress(Globals.HeapFunctions.AnalyzeHeapCorruption(ExceptionThread).Base);
			text3 = text3 + "<div class='summaryItemCallout'>Heap corruption was detected in this dump</div>Please follow up with the vendor of the Module listed in the recommendation section for further assistance on this issue.<br><br>Current NTGlobalFlags value: <b>0x" + Globals.g_GlobalFlagsValue + " (" + Globals.HeapFunctions.GetGlobalFlagDescription(Globals.g_GlobalFlagsValue) + ")</b>";
			text2 = "The heap corruption detected in this dump was likely caused by the following Module:<br><br><b>" + moduleByAddress.ImageName + "</b>";
			text2 = (string.IsNullOrEmpty(moduleByAddress.VSCompanyName) ? (text2 + "<br><br>Please follow up with the vendor of this Module for further assitance.") : (text2 + "<br><br>Please follow up with the following vendor regarding this issue:<br><br><b>" + moduleByAddress.VSCompanyName + "</b>"));
		}
		Globals.Manager.Write(text3);
		if (!suppressSummary)
		{
			Globals.Manager.ReportError(text3, text2, 1000, "{1c9a892f-c1fb-4b26-92f0-a0963ebf0bcb}");
		}
		if (moduleByAddress != null && Globals.g_Debugger.IsCrashDump)
		{
			Globals.Manager.Write("<BR><BR>");
			Globals.HelperFunctions.ModuleInfo(moduleByAddress);
		}
	}

	public static bool SMTPBugCheck(CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		bool flag = false;
		double num = 0.0;
		double num2 = 0.0;
		bool flag2 = false;
		string text = "";
		string text2 = "";
		string text3 = "";
		num = ExceptionThread.StackAddress;
		flag2 = true;
		for (int i = 0; i <= 9; i++)
		{
			if ((double)Globals.g_Debugger.ReadWord(num) != 38469.0)
			{
				flag2 = false;
				break;
			}
			num += 4.0;
		}
		if (flag2)
		{
			text = "<br><b>Symptoms of a known issue with the SMTP service were found in " + Globals.g_ShortDumpFileName + "</b><br>";
			num2 = ExceptionThread.Register("esi");
			text2 = Globals.g_Debugger.ReadANSIString(num2 + 88.0);
			text3 = Convert.ToString(Globals.g_Debugger.ReadByte(num2 + 60.0));
			for (int j = 1; j <= 3; j++)
			{
				text3 = text3 + "." + Convert.ToString(Globals.g_Debugger.ReadByte(num2 + 60.0 + (double)j));
			}
			text = text + "<b>Remote machine: " + text2 + " (" + text3 + ")</b><br><br>";
			if (!suppressSummary)
			{
				Globals.Manager.ReportError(text, "Apply the Exchange hotfix referenced in <b><a href='http://support.microsoft.com/?id=827214' target='_blank'>827214</a></b> to resolve this issue", 1000, "{eb767ba1-0aa7-443c-b2d2-dc29a095a0d2}");
			}
			Globals.HelperFunctions.ModuleInfo(Globals.g_Debugger.GetModuleByModuleName("SMTPSVC"));
			return true;
		}
		return false;
	}

	public static IDbgModule AnalyzeStringAV(CacheFunctions.ScriptThreadClass ExceptionThread)
	{
		int num = 0;
		string text = "";
		IDbgModule result = null;
		text = "MSVCRT!WCSLEN;NTDLL!WCSLEN;USER32!WSPRINTFA;USER32!WVSPRINTFA;KERNEL32!LSTRLENW";
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = ExceptionThread.StackFrames;
		for (num = 0; num <= Convert.ToInt32(stackFrames.Count) - 1; num++)
		{
			CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = stackFrames[num];
			string text2 = scriptStackFrameClass.GetFrameText().ToUpper();
			if (text.IndexOf(text2.ToUpper(), 0) == -1)
			{
				return Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.InstructionAddress);
			}
		}
		return result;
	}

	private static IDbgModule AnalyzeExceptionLoop(CacheFunctions.ScriptThreadClass Thread)
	{
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		int num = 0;
		NetDbgException val = null;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> stackFrames = Thread.StackFrames;
		for (num = Convert.ToInt32(stackFrames.Count) - 1; num >= 0; num += -1)
		{
			scriptStackFrameClass = stackFrames[num];
			if (scriptStackFrameClass.GetFrameText().ToUpper().IndexOf("NTDLL!KIUSEREXCEPTIONDISPATCHER", 0) >= 0)
			{
				val = Globals.g_Debugger.GetExceptionObjectFromAddress(scriptStackFrameClass.Args(2));
				string[] array = Globals.HelperFunctions.Split(Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(val.ExceptionAddress)).ToUpper(), "+", -1);
				if (array[0].IndexOf("KERNEL32!RAISEEXCEPTION", 0) == -1 && array[0].IndexOf("KERNELBASE!RAISEEXCEPTION", 0) == -1)
				{
					return Globals.g_Debugger.GetModuleByAddress(val.ExceptionAddress);
				}
				scriptStackFrameClass = stackFrames[num + 1];
				return Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.ReturnAddress);
			}
		}
		return Globals.g_Debugger.GetModuleByAddress(Globals.g_Debugger.NativeException.ExceptionAddress);
	}

	private static string GetFaultingThreadIDWithLink(int exceptionThreadId)
	{
		if (Globals.Manager.DoHangAnalysisOnCrashDumps)
		{
			return Globals.HelperFunctions.GetThreadIDWithLink(exceptionThreadId);
		}
		return $"<a href='#FaultingThread{Globals.g_UniqueReference}'>{exceptionThreadId}</a>";
	}

	private static void AnalyzeUnknown(NetDbgException ExceptionObj, CacheFunctions.ScriptThreadClass ExceptionThread, bool suppressSummary)
	{
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		string text = null;
		string text2 = "";
		string text3 = "";
		string text4 = null;
		IDbgModule val = null;
		double num = 0.0;
		string text5 = "";
		string text6 = "";
		text4 = Globals.g_Debugger.GetAs32BitHexString(ExceptionObj.ExceptionCode);
		text = Globals.g_Debugger.GetSymbolFromAddress(ExceptionObj.ExceptionAddress);
		IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(ExceptionObj.ExceptionAddress);
		text2 = "In " + Convert.ToString(Globals.g_ShortDumpFileName) + " the assembly instruction at <b>" + text;
		if (moduleByAddress == null)
		{
			text2 += "</b> which does not correspond to any known native module in the process";
			text3 = "Please contact Microsoft Corporation for troubleshooting steps on stack corruption<br>";
		}
		else
		{
			text2 = text2 + "</b> in <b>" + Convert.ToString(moduleByAddress.ImageName);
			if (Convert.ToString(moduleByAddress.VSCompanyName) != "")
			{
				text2 = text2 + "</b> from <b>" + Convert.ToString(moduleByAddress.VSCompanyName);
			}
			if (text.ToUpper().IndexOf("KERNEL32!RAISEEXCEPTION", 0) != -1)
			{
				text6 = "Kernel32!RaiseException";
				num = Globals.HelperFunctions.GetDirectCaller(ExceptionThread.StackFrames, "KERNEL32!RAISEEXCEPTION", 0);
			}
			else if (text.ToUpper().IndexOf("KERNELBASE!RAISEEXCEPTION", 0) != -1)
			{
				text6 = "KernelBase!RaiseException";
				num = Globals.HelperFunctions.GetDirectCaller(ExceptionThread.StackFrames, "KERNELBASE!RAISEEXCEPTION", 0);
			}
			if (text6 != "")
			{
				if (num > 0.0)
				{
					text3 = "Review the faulting call stack for thread " + Convert.ToString(GetFaultingThreadIDWithLink(ExceptionThread.ThreadID)) + " to determine root cause for the exception.<br>" + Convert.ToString(Globals.HelperFunctions.GetVendorMessage(num));
					text = Globals.HelperFunctions.GetFunctionNameNoUpper(num);
				}
				else
				{
					text3 = "<br>An exception thrown by <Font Color='Red'><b>" + text6 + "</b></Font> usually indicates a problem with another module. Please review the stack for the faulting thread (" + Convert.ToString(GetFaultingThreadIDWithLink(ExceptionThread.ThreadID)) + ") further to determine which module actually threw the exception raised by Kernel32.dll.<br><br>";
				}
			}
			else if (text.ToUpper().IndexOf("NTDLL!KIRAISEUSEREXCEPTIONDISPATCHER", 0) != -1)
			{
				text5 = "ADVAPI32!REGCLOSEKEY;KERNEL32!CLOSEHANDLE";
				dictionary = ExceptionThread.StackFrames;
				for (int i = 0; i <= Convert.ToInt32(dictionary.Count) - 1; i++)
				{
					scriptStackFrameClass = dictionary[i];
					string text7 = scriptStackFrameClass.GetFrameText().ToUpper();
					if (text5.IndexOf(text7.ToUpper(), 0) != -1)
					{
						val = Globals.g_Debugger.GetModuleByAddress(scriptStackFrameClass.ReturnAddress);
						num = scriptStackFrameClass.ReturnAddress;
						text3 = "This exception occured as a result of an invalid handle passed to " + text7 + " by the following function:<br><br><b>" + Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(scriptStackFrameClass.ReturnAddress)) + "</b><br><br>Please follow up with the vendor of this module";
						if (Convert.ToString(val.VSCompanyName) != "")
						{
							text3 = text3 + ", <b>" + Convert.ToString(val.VSCompanyName) + "</b>,";
						}
						text3 += " for further assistance with this issue.";
						moduleByAddress = Globals.g_Debugger.GetModuleByAddress(num);
					}
				}
			}
			else
			{
				text3 = Convert.ToString(Globals.HelperFunctions.GetVendorMessage(ExceptionObj.ExceptionAddress));
			}
		}
		if (Convert.ToInt64(num) > 0)
		{
			text2 = text2 + "<br>This exception originated from <b><font color='darkblue'>" + text + "</font></b>. ";
		}
		text2 = text2 + "</b> has caused an <b>unknown exception (<font color='red'>" + Convert.ToString(text4) + "</font>)</b> on thread " + Convert.ToString(GetFaultingThreadIDWithLink(ExceptionThread.ThreadID)) + "<br>";
		if (Convert.ToInt64(num) > 0)
		{
			text2 = text2 + "<br>This exception originated from <b>" + Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(num)) + "</b>. ";
		}
		Globals.Manager.Write(text2);
		if (!suppressSummary)
		{
			Globals.Manager.ReportError(text2, text3, 1000, "{ed0d97e1-fca7-49e5-9d3c-47e2180f263c}");
		}
		if (moduleByAddress != null && Globals.g_Debugger.IsCrashDump)
		{
			Globals.HelperFunctions.ModuleInfo(moduleByAddress);
		}
	}
}
