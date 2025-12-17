using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.Reports;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public class AnalyzeThreads
{
	internal class AdoOffsets
	{
		public double m_lCursorType;

		public double m_eCursorLocation;

		public double m_fOpen;

		public double m_sstrSource;

		public double m_strSQL;

		public double m_lQueryTimeout;

		public double m_lLoginTimeout;

		public double m_connInfo;

		public double m_lCommandTimeout;
	}

	private enum ADOState
	{
		adStateClosed,
		adStateOpen,
		unknown
	}

	private enum ADOCursorLocation
	{
		adUseNone = 1,
		adUseServer = 2,
		adUseClient = 3,
		adUseServer_unspecified = -1,
		unknown = 0
	}

	private enum ADOCursorType
	{
		adOpenForwardOnly = 0,
		adOpenKeyset = 1,
		adOpenDynamic = 2,
		adOpenStatic = 3,
		adOpenUnspecified = -1,
		unknown = 0
	}

	private const string SHORT_DESC_UNRESOLVED = "not fully resolved and may or may not be a problem";

	private const string SHORT_DESC_UNRESOLVED_CLIENT_REQUEST = "processing a client request and is/are not fully resolved";

	private const string SHORT_DESC_CRITSEC = "waiting on critical section";

	private const string SHORT_DESC_LOADLIB = "loading <b>";

	private const string SHORT_DESC_COM = "making a COM call";

	private const string SHORT_DESC_RPCCALLOUT = "making an outbound RPC call";

	private const string SHORT_DESC_RPCCALLIN = "processing an inbound RPC call";

	private const string SHORT_DESC_MESSAGEBOX = "displaying a <b>message box</b>";

	private const string SHORT_DESC_ASPDEBUG = "hung due to ASP server side script debugging";

	private const string SHORT_DESC_WRITECLIENT = "returning data to the client using the WriteClient API";

	private const string SHORT_DESC_CRL = "checking the certificate revocation lists (CRLs) to verify that a certificate is still valid";

	private const string SHORT_DESC_ADO = "making a database operation using ADO";

	private const string SHORT_DESC_WININET = "making a network using WinInet library";

	private const string SHORT_DESC_WINHTTP = "making a network using WinHTTP library";

	private const string SHORT_DESC_SOCKET_DATA = "waiting on data to be returned from another server via WinSock";

	private const string SHORT_DESC_SOCKET_FUNCTION = "waiting on a socket function to complete";

	private const string SHORT_DESC_BADTEB = "incomplete and also has/have an invalid Thread Environment Block pointer";

	private const string SHORT_DESC_EXCEPTIONFILTER = "blocked by an <b>unhandled exception</b>";

	private const string SHORT_DESC_ISAPI_FILTER = "calling an ISAPI Filter";

	private const string SHORT_DESC_ISAPI_EXTENSION = "calling an ISAPI Extension";

	private const string SHORT_DESC_IISINTRINSICS_MARSHAL = "making a callback to IIS to marshal the <b>IIS Intrinsic Objects</b>";

	private const string SHORT_DESC_IISINTRINSICS_UNMARSHAL = "making a callback to IIS to unmarshal the <b>IIS Intrinsic Objects</b>";

	private const string SHORT_DESC_SLEEP = "calling the <b>Sleep</b> API";

	private const string SHORT_DESC_TXCOMMIT = "attempting to commit a DTC transaction";

	private const string SHORT_DESC_TXVOTE = "attempting to vote on a DTC transaction";

	private const string SHORT_DESC_OK = "";

	public const string SHORT_DESC_CRITSEC_REPLACE = "waiting on a critical section";

	private const string SHORT_DESC_LOADLIB_REPLACE = "loading a dll using the LoadLibrary API";

	public const string SHORT_DESC_UNRESOLVED_REPLACE = "not fully resolved and may or may not be a problem.  Further analysis of this thread may be required";

	private const string SHORT_DESC_BADTEB_REPLACE = "incomplete and also has an invalid Thread Environment Block pointer";

	private const string LPC_BOLD = "<b>LRPC</b>";

	private const string SHORT_GC_RECOMMENDATION = "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may ! be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmentation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details";

	private const string SHORT_GC_RECOMMENDATION_PREMPTIVE_THREADS = "Review the callstacks for the threads that have preemptive GC Disabled to see what they are doing which is preventing the GC to run. Review the blog <a href='http://blogs.msdn.com/tess/archive/2007/03/12/net-hang-case-study-the-gc-loader-lock-deadlock-a-story-of-mixed-mode-dlls.aspx'>.NET Hang Case Study: The GC-Loader Lock Deadlock (a story of mixed mode dlls)</a> that gives some information on how to debug these issue more";

	private const string SHORT_THREAD_SPINNING_TO_ENTER_A_LOCK = "spinning waiting to enter a .net lock";

	private const string SHORT_THREAD_WAITING_TO_ENTER_A_LOCK = "waiting to enter a .NET Lock";

	private const string SHORT_THREAD_WAITING_WAITMULTIPE = "waiting in a WaitMultiple";

	private const string SHORT_THREAD_WAITING_ON_DB_SERVER = "waiting on data to be returned from the database server";

	private const string SHORT_THREAD_OPENING_DB_CONNECTION = "trying to open a data base connection";

	private const string SHORT_THREAD_WAITING_WAITONE = "waiting in a WaitOne";

	public const string SHORT_DESC_COMCALL = "making a COM call";

	private const string SHORT_DESC_COMCALL_ACTIVATION = "waiting on the local <b>RpcSs service</b>";

	private const string SHORT_RECOMMENDATION_THREAD_WAITING_DOTNET = "Typically threads waiting in WaitMultiple are monitoring threads in a process and this may be ignored, however too many threads waiting in WaitOne\\ WaitMultiple may be a problem. Review the callstack of the threads waiting to see what they are waiting on";

	public static void PreloadThreadData(int dumpNum, int totalDumps, bool hashStacks = true)
	{
		int num = 0;
		string text = null;
		int num2 = 0;
		int num3 = 0;
		if (Globals.g_MinThreadOverride == -1)
		{
			num2 = 0;
			num3 = Globals.g_ThreadInfoCache.Count - 1;
		}
		else
		{
			num2 = Globals.g_MinThreadOverride;
			num3 = Globals.g_MaxThreadOverride;
		}
		if (dumpNum > 0)
		{
			text = " - Dump #" + dumpNum + " of " + totalDumps;
		}
		Globals.HelperFunctions.ResetStatus("Preloading thread data" + text, num3, "Thread");
		for (num = num2; num <= num3; num++)
		{
			CacheFunctions.ScriptThreadClass scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
			scriptThreadClass.HashStack = hashStacks;
			_ = scriptThreadClass.StackFrames;
			Globals.AnalyzeManaged.IsClrExtensionExecuting();
			_ = scriptThreadClass.StartAddress;
			_ = scriptThreadClass.StartAddressSymbol;
			_ = scriptThreadClass.SystemID;
			_ = scriptThreadClass.CreateTime;
			scriptThreadClass.GetUserTime(out var Days, out var Hours, out var Minutes, out var Seconds, out var MilliSeconds);
			scriptThreadClass.GetKernelTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
			Globals.HelperFunctions.IncrementSubStatus();
		}
	}

	public object SetAdoOffsets()
	{
		IDbgModule val = null;
		val = Globals.g_Debugger.GetModuleByModuleName("MSADO15");
		AdoOffsets adoOffsets;
		if (val == null)
		{
			adoOffsets = null;
			return null;
		}
		adoOffsets = new AdoOffsets();
		adoOffsets.m_eCursorLocation = 256.0;
		adoOffsets.m_fOpen = 520.0;
		adoOffsets.m_sstrSource = 524.0;
		adoOffsets.m_lCursorType = 624.0;
		adoOffsets.m_strSQL = 80.0;
		adoOffsets.m_lQueryTimeout = 104.0;
		adoOffsets.m_connInfo = 100.0;
		adoOffsets.m_lCommandTimeout = 156.0;
		adoOffsets.m_lLoginTimeout = 160.0;
		object obj = default(object);
		object obj2 = default(object);
		object obj3 = default(object);
		object obj4 = default(object);
		val.GetFileVersion(out obj, out obj2, out obj3, out obj4);
		int num = (int)obj;
		int num2 = (int)obj2;
		_ = (int)obj3;
		_ = (int)obj4;
		switch (num2)
		{
		case 50:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 532.0;
			adoOffsets.m_sstrSource = 536.0;
			adoOffsets.m_lCursorType = 636.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 92.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 51:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 532.0;
			adoOffsets.m_sstrSource = 536.0;
			adoOffsets.m_lCursorType = 636.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 92.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 52:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 532.0;
			adoOffsets.m_sstrSource = 536.0;
			adoOffsets.m_lCursorType = 636.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 92.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 53:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 532.0;
			adoOffsets.m_sstrSource = 536.0;
			adoOffsets.m_lCursorType = 636.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 92.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 60:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 524.0;
			adoOffsets.m_sstrSource = 528.0;
			adoOffsets.m_lCursorType = 628.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 61:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 516.0;
			adoOffsets.m_sstrSource = 520.0;
			adoOffsets.m_lCursorType = 620.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 62:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 516.0;
			adoOffsets.m_sstrSource = 520.0;
			adoOffsets.m_lCursorType = 620.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 70:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 516.0;
			adoOffsets.m_sstrSource = 520.0;
			adoOffsets.m_lCursorType = 620.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 71:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 516.0;
			adoOffsets.m_sstrSource = 520.0;
			adoOffsets.m_lCursorType = 620.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 80:
			adoOffsets.m_eCursorLocation = 252.0;
			adoOffsets.m_fOpen = 516.0;
			adoOffsets.m_sstrSource = 520.0;
			adoOffsets.m_lCursorType = 620.0;
			adoOffsets.m_strSQL = 76.0;
			adoOffsets.m_lQueryTimeout = 100.0;
			adoOffsets.m_connInfo = 96.0;
			adoOffsets.m_lCommandTimeout = 152.0;
			adoOffsets.m_lLoginTimeout = 156.0;
			break;
		case 81:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 520.0;
			adoOffsets.m_sstrSource = 524.0;
			adoOffsets.m_lCursorType = 624.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 104.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 82:
			adoOffsets.m_eCursorLocation = 256.0;
			adoOffsets.m_fOpen = 520.0;
			adoOffsets.m_sstrSource = 524.0;
			adoOffsets.m_lCursorType = 624.0;
			adoOffsets.m_strSQL = 80.0;
			adoOffsets.m_lQueryTimeout = 104.0;
			adoOffsets.m_connInfo = 100.0;
			adoOffsets.m_lCommandTimeout = 156.0;
			adoOffsets.m_lLoginTimeout = 160.0;
			break;
		case 0:
			if (num == 6)
			{
				adoOffsets.m_eCursorLocation = 268.0;
				adoOffsets.m_fOpen = 532.0;
				adoOffsets.m_sstrSource = 536.0;
				adoOffsets.m_lCursorType = 624.0;
				adoOffsets.m_strSQL = 80.0;
				adoOffsets.m_lQueryTimeout = 104.0;
				adoOffsets.m_connInfo = 116.0;
				adoOffsets.m_lCommandTimeout = 176.0;
				adoOffsets.m_lLoginTimeout = 180.0;
			}
			break;
		}
		return null;
	}

	public void DoAnalyzeThreads()
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		int num = 0;
		AnalyzedThreadClass analyzedThreadClass = null;
		int num2 = 0;
		double num3 = 0.0;
		Globals.HelperFunctions.ResetStatus("Scanning for known blocking causes", Globals.g_ThreadInfoCache.Count, "Thread");
		AnalyzedThreadsClass analyzedThreadsClass = (Globals.g_AnalyzedThreads = new AnalyzedThreadsClass());
		SetAdoOffsets();
		if (Globals.g_MinThreadOverride == -1)
		{
			num2 = 0;
			num3 = Globals.g_ThreadInfoCache.Count - 1;
		}
		else
		{
			num2 = Globals.g_MinThreadOverride;
			num3 = Globals.g_MaxThreadOverride;
		}
		for (num = num2; (double)num <= num3; num++)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.Item(num);
			analyzedThreadClass = null;
			if (analyzedThreadsClass.Exists(scriptThreadClass.ThreadID))
			{
				analyzedThreadClass = analyzedThreadsClass.Item(scriptThreadClass.ThreadID);
			}
			else
			{
				analyzedThreadClass = getAnalysis(scriptThreadClass);
				if (!analyzedThreadsClass.Exists(scriptThreadClass.ThreadID) && analyzedThreadClass != null)
				{
					analyzedThreadsClass.Add(analyzedThreadClass);
				}
			}
			if (analyzedThreadClass != null)
			{
				AppendPreviousExceptionInfo(ref analyzedThreadClass);
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		if (!Globals.g_IsBlockingIssueDetected)
		{
			Globals.g_IsBlockingIssueDetected = ScanAnalyzedThreadsForKnownIssues();
		}
	}

	public bool ScanAnalyzedThreadsForKnownIssues()
	{
		foreach (AnalyzedThreadClass value in Globals.g_AnalyzedThreads.Summaries.Values)
		{
			if (value.IsIssue)
			{
				return true;
			}
		}
		return false;
	}

	public void AppendPreviousExceptionInfo(ref AnalyzedThreadClass AnalyzedThread)
	{
		string text = null;
		string text2 = null;
		AnalyzedThreadClass _a = AnalyzedThread;
		if (!Globals.g_collPreviousExceptions.ContainsKey(AnalyzedThread.Thread.ThreadID.ToString()))
		{
			return;
		}
		int num = Globals.g_collPreviousExceptions.Count((KeyValuePair<string, string> t) => t.Key == _a.Thread.ThreadID.ToString());
		if (num == 1)
		{
			text = "<Font Color='Red'><b>Recovered Call Stack</b></Font> for thread " + AnalyzedThread.Thread.ThreadID + "<br><br>" + Globals.g_collPreviousExceptions[AnalyzedThread.Thread.ThreadID + ":1"];
		}
		else
		{
			for (int i = 1; i <= num; i++)
			{
				text = text + "<Font Color='Red'><b>Recovered Call Stack #" + i + "</b></Font> for thread " + AnalyzedThread.Thread.ThreadID + "<br><br>" + Globals.g_collPreviousExceptions[AnalyzedThread.Thread.ThreadID + ":" + i];
				if (i < num)
				{
					text += "<br><br>";
				}
			}
		}
		text2 = AnalyzedThread.AdditionalInfo;
		if (text2.GetSafeLength() > 0)
		{
			text2 += "<br><br>";
		}
		AnalyzedThread.AdditionalInfo = text2 + text;
	}

	public AnalyzedThreadClass getExceptionFilterAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		if (Globals.g_OSPlatformVersion == "X64")
		{
			return null;
		}
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		AnalyzedThreadClass analyzedThreadClass = null;
		NetDbgException val = null;
		int num = 0;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		dictionary = Thread.StackFrames;
		scriptStackFrameClass = dictionary[FrameID];
		for (num = dictionary.Count - 1; num >= 0; num--)
		{
			scriptStackFrameClass = dictionary[num];
			string text = scriptStackFrameClass.GetFrameText().ToUpper();
			if (text.IndexOf(Globals.g_COMRuntimeModule + "!APPINVOKEEXCEPTIONFILTER") >= 0 && Globals.g_OSVER == Globals.OS_VER_WIN2K)
			{
				ulong num2 = (ulong)scriptStackFrameClass.Args(0);
				ulong num3 = Globals.g_Debugger.ReadDWord(num2);
				analyzedThreadClass.KeyPartOne = Convert.ToString(num3);
				num2 += 4;
				ulong num4 = Globals.g_Debugger.ReadDWord(num2);
				analyzedThreadClass.KeyPartTwo = Convert.ToString(num4);
				break;
			}
			if (text.IndexOf("KERNEL32!UNHANDLEDEXCEPTIONFILTER") >= 0 || text.IndexOf("COMSVCS!COMSVCSEXCEPTIONFILTER") >= 0)
			{
				ulong num2 = (ulong)scriptStackFrameClass.Args(0);
				ulong num3 = Globals.g_Debugger.ReadDWord(num2);
				analyzedThreadClass.KeyPartOne = Convert.ToString(num3);
				num2 += 4;
				ulong num4 = Globals.g_Debugger.ReadDWord(num2);
				analyzedThreadClass.KeyPartTwo = Convert.ToString(num4);
				break;
			}
			if (text.IndexOf("NTDLL!RTLDISPATCHEXCEPTION") >= 0)
			{
				analyzedThreadClass.KeyPartOne = scriptStackFrameClass.Args(0).ToString();
				analyzedThreadClass.KeyPartTwo = scriptStackFrameClass.Args(1).ToString();
				break;
			}
		}
		analyzedThreadClass.Category = "EXCEPTIONFILTER";
		analyzedThreadClass.IsError = true;
		analyzedThreadClass.Description = "blocked by an <b>unhandled exception</b>";
		HelperFunctionsImpl helperFunctionsImpl = new HelperFunctionsImpl();
		analyzedThreadClass.Recommendation = "Please see the <Font Color='Red'><b>Recovered Call Stack</b></Font> for thread " + helperFunctionsImpl.GetThreadIDWithLink(Thread.ThreadID) + " based on the restored exception and context record passed to the exception filter.";
		if (!Globals.g_collPreviousExceptions.ContainsKey(analyzedThreadClass.Thread.ThreadID.ToString()))
		{
			Globals.g_collExceptionThreads.Add(Thread.ThreadID, Thread);
		}
		val = Globals.g_Debugger.GetExceptionObjectFromAddress(Convert.ToDouble(analyzedThreadClass.KeyPartOne));
		if (val == null)
		{
			analyzedThreadClass.AdditionalInfo = "Could not restore the exception information passed to the exception filter. Crash analysis unavailable.<br>";
		}
		else
		{
			double fContextAdress = Convert.ToDouble(analyzedThreadClass.KeyPartTwo);
			if (Thread.ChangeThreadContext(ref fContextAdress))
			{
				Thread.FlushStackFrames();
				analyzedThreadClass.AdditionalInfo = "<Font Color='Red'><b>Recovered stack for thread " + Thread.ThreadID + "</b></Font><br><br>" + Thread.StackReportWithArgs;
				AnalyzeCrash.AnalyzeException(val, analyzedThreadClass.Thread, suppressSummary: true);
				Globals.g_Debugger.Execute("~0s;~1s");
				Globals.g_Debugger.Execute("~" + Thread.ThreadID + "s");
				Globals.g_Debugger.Execute(".cxr");
				Thread.FlushStackFrames();
			}
			else
			{
				analyzedThreadClass.AdditionalInfo = "Could not restore the context information passed to the exception filter. Crash analysis unavailable.<br>";
			}
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass GetTxCommitAnalysis(ref CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "TXCOMMIT",
			IsError = false,
			IsWarning = true,
			Description = "attempting to commit a DTC transaction.  Various issues can cause a transaction to commit slowly or to block until timeout.  Examples include:<ol><li>Delays internal to any Resource Manager (RM), such as a database server, participating in the transaction</li><li>Delays internal to any client application participating in the transaction</li><li>Delays in the network between the Transaction Coordinator (MSDTC), and any RM or client participating in the transaction</li></ol>",
			Recommendation = "Each scenario in the Description pane to the left would require different troubleshooting steps.  Examples include:<ol><li>Use appropriate profiling utilities (i.e. SQL Profiler for SQL Server) and/or use DebugDiag to capture and analyze Userdumps of the processes hosting the Resource Managers during the transaction</li><li>Use network utilities (i.e. Netmon) to capture and analyze network traffic between MSDTC and the RMs and clients</li><li>Use DebugDiag to capture and analyze Userdumps of the processes hosting the client applications during the transaction, to ensure that no issues are preventing the clients from successfully voting on the transaction</li></ol>"
		};
	}

	public AnalyzedThreadClass GetTxVoteAnalysis(ref CacheFunctions.ScriptThreadClass Thread, object FrameID)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "TXVOTE",
			IsError = false,
			IsWarning = true,
			Description = "attempting to vote on a DTC transaction.  This transaction will not be able to commit until this client application is able to complete its vote. Any other application will block during this time if it attempts to commit the transaction, and the transaction will eventually be aborted if the transaction timeout is exceeded (default is 60 seconds).",
			Recommendation = "Review the call stack for this thread and any recommendations in this report pertaining to this thread, in order to determine what may be blocking this vote from completing."
		};
	}

	public AnalyzedThreadClass getComSvcsExceptionFilterAnalysis(ref CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		AnalyzedThreadClass analyzedThreadClass = null;
		if (Globals.g_OSVER == Globals.OS_VER_WIN2K)
		{
			dictionary = Thread.StackFrames;
			for (int i = FrameID + 1; i <= Math.Min(FrameID + 5, dictionary.Count); i++)
			{
				if (CacheFunctions.GetFunctionName(dictionary[i].InstructionAddress) == Globals.g_COMRuntimeModule + "!APPINVOKEEXCEPTIONFILTER")
				{
					analyzedThreadClass = getExceptionFilterAnalysis(Thread, i);
					break;
				}
			}
		}
		if (analyzedThreadClass == null)
		{
			analyzedThreadClass = getExceptionFilterAnalysis(Thread, FrameID);
		}
		analyzedThreadClass.Description += " which caused a <font color=red><B>COM+ FailFast</B></font> to occur.";
		analyzedThreadClass.Weight = 2000;
		return analyzedThreadClass;
	}

	public bool ShouldIgnoreComCall(AnalyzedThreadClass ProblemSummary)
	{
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		bool flag = false;
		if (ProblemSummary.BlockedThreads.Count == 0)
		{
			return true;
		}
		using (Dictionary<int, CacheFunctions.ScriptThreadClass>.ValueCollection.Enumerator enumerator = ProblemSummary.BlockedThreads.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				scriptThreadClass = enumerator.Current;
			}
		}
		int num = ((scriptThreadClass.COMDestinationProcessID != Globals.g_Debugger.ProcessID) ? (-1) : Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(scriptThreadClass.COMDestinationThreadID));
		if (Globals.g_collThreadsBlockedByCritsecs.ContainsKey(num) && Globals.g_MixedComcallCritsecDeadlockDetected)
		{
			ProblemSummary.Recommendation = ProblemSummary.Recommendation + "<br><br><b>Note:</b> thread " + Globals.HelperFunctions.GetThreadIDWithLink(num) + " is involved in a \"combined critical section / COM call\" problem described in another Error item in this report summary.  Resolving this COM call problem by following the guidance above may resolve both problems.";
			return false;
		}
		if (ProblemSummary.IsWarning && ProblemSummary.BlockedThreads.Count < 2)
		{
			flag = true;
			if (ProblemSummary.BlockedThreads.Count > 0)
			{
				if (scriptThreadClass.ThreadID == 0)
				{
					flag = false;
				}
				else
				{
					foreach (KeyValuePair<int, CacheFunctions.ScriptThreadClass> blockedThread in ProblemSummary.BlockedThreads)
					{
						_ = blockedThread;
						if (Globals.AnalyzeComPlus.IsWellKnownCOMSTA(scriptThreadClass.SystemID))
						{
							flag = false;
							break;
						}
					}
				}
			}
			if (flag)
			{
				return true;
			}
		}
		if (scriptThreadClass.COMDestinationProcessID != Globals.g_Debugger.ProcessID)
		{
			return false;
		}
		if (Globals.g_collThreadsBlockedByCritsecs.ContainsKey(num))
		{
			return true;
		}
		if (Globals.g_IsComPlusSTAPoolIssueDetected && Globals.g_collCOMPlusSTAThreadPoolThreads.ContainsKey(num))
		{
			return true;
		}
		return false;
	}

	public void ReportThread(AnalyzedThreadClass AnalyzedThread)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		ThreadReportSection val = new ThreadReportSection("Thread" + AnalyzedThread.Thread.SystemID, true);
		Globals.Manager.CurrentSection.AddChildSection((ReportSection)(object)val);
		((ReportSection)val).Title = "Thread " + AnalyzedThread.Thread.ThreadID + " - System ID " + AnalyzedThread.Thread.SystemID;
		if (Globals.g_ExtendedThreadInfoAvailable)
		{
			val.WriteHeader("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
			if (AnalyzedThread.Thread.StartAddress != 0.0)
			{
				val.WriteHeader("<tr><td>Entry point</td><td>&nbsp;&nbsp;<b>" + Globals.g_Debugger.GetSymbolFromAddress(AnalyzedThread.Thread.StartAddress) + "</b></td></tr>");
			}
			val.WriteHeader(string.Concat("<tr><td>Create time</td><td>&nbsp;&nbsp;<b>", AnalyzedThread.Thread.CreateTime, "</b></td></tr>"));
			AnalyzedThread.Thread.GetUserTime(out var Days, out var Hours, out var Minutes, out var Seconds, out var MilliSeconds);
			val.WriteHeader("<tr><td>Time spent in user mode</td><td>&nbsp;&nbsp;<b>" + Days + " Days " + Globals.HelperFunctions.PadZero(Convert.ToInt16(Hours)) + ":" + Globals.HelperFunctions.PadZero(Convert.ToInt16(Minutes)) + ":" + Globals.HelperFunctions.PadZero(Convert.ToInt16(Seconds)) + "." + Globals.HelperFunctions.PadZero2(Convert.ToInt16(MilliSeconds)) + "</b></td></tr>");
			AnalyzedThread.Thread.GetKernelTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
			val.WriteHeader("<tr><td>Time spent in kernel mode</td><td>&nbsp;&nbsp;<b>" + Days + " Days " + Globals.HelperFunctions.PadZero(Convert.ToInt16(Hours)) + ":" + Globals.HelperFunctions.PadZero(Convert.ToInt16(Minutes)) + ":" + Globals.HelperFunctions.PadZero(Convert.ToInt16(Seconds)) + "." + Globals.HelperFunctions.PadZero2(Convert.ToInt16(MilliSeconds)) + "</b></td></tr>");
			val.WriteHeader("</table><br><br>");
		}
		if (Convert.ToString(AnalyzedThread.Description).GetSafeLength() > 0)
		{
			val.WriteHeader("This thread is " + AnalyzedThread.Description + "<br><br>");
		}
		if (Globals.g_ThreadInfoCache.Count == 1)
		{
			Globals.g_Debugger.Execute(".ecxr");
		}
		scriptThreadClass = AnalyzedThread.Thread;
		if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
		{
			if (Convert.ToString(AnalyzedThread.AdditionalCLRInfo).GetSafeLength() > 0)
			{
				val.WriteHeader(AnalyzedThread.AdditionalCLRInfo);
			}
			if (Globals.g_ThreadExceptionList.ContainsKey(AnalyzedThread.Thread.ThreadID))
			{
				val.WriteHeader("<b>The thread has evidence of <u>.net exceptions</u> on the stack. Check the <a href='#ManagedExceptionsInStacksReport" + Globals.g_UniqueReference + "'>Previous .NET Exceptions Report (Exceptions in all .NET Thread Stacks)</a>  to view more details of the associated exception </b> <br><br>");
			}
			if (Convert.ToString(scriptThreadClass.ClrStackReportNoArgs).GetSafeLength() != 0)
			{
				((ReportSection)val).Write("<b>.NET Call Stack</b><br><br>");
				((ReportSection)val).Write(scriptThreadClass.ClrStackReportNoArgs);
				((ReportSection)val).Write("<BR>");
				((ReportSection)val).Write("<b>Full Call Stack</b><br><br>");
			}
			else
			{
				((ReportSection)val).Write("<BR>");
				((ReportSection)val).Write("<b>Call Stack</b><br><br>");
			}
		}
		((ReportSection)val).Write(scriptThreadClass.StackReportNoArgs);
		val.ThreadHash = scriptThreadClass.ThreadHash;
		if (!string.IsNullOrEmpty(AnalyzedThread.AdditionalInfo))
		{
			((ReportSection)val).Write(AnalyzedThread.AdditionalInfo);
		}
		IASPRequest aSPRequestByThreadID = Globals.g_ASPInfo.GetASPRequestByThreadID(AnalyzedThread.Thread.ThreadID);
		if (aSPRequestByThreadID != null)
		{
			ReportSection currentSection = Globals.Manager.CurrentSection;
			Globals.Manager.CurrentSection = (ReportSection)(object)val;
			Globals.ReportASPInfo.ReportASPRequest(aSPRequestByThreadID);
			Globals.ReportASPInfo.OutputVBScriptStack(aSPRequestByThreadID);
			Globals.Manager.CurrentSection = currentSection;
		}
	}

	public void ReportThreads()
	{
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected O, but got Unknown
		bool flag = false;
		Globals.HelperFunctions.ResetStatus("Reporting summary information", Globals.g_AnalyzedThreads.Summaries.Count, "Problem");
		foreach (AnalyzedThreadClass value in Globals.g_AnalyzedThreads.Summaries.Values)
		{
			flag = false;
			string category = value.Category;
			if (!(category == "COMCALL"))
			{
				_ = category == "COMCALLSTA";
			}
			if (value.Category.EndsWith("_HASWAITERS"))
			{
				if (!value.IsWarning && !value.IsError)
				{
					value.IsWarning = true;
				}
				value.Weight *= 10;
				string text = string.Format("<br><br><div class='summaryItemCallout'>Note: {0} blocked while also blocking other threads.  This can lead to slow performance or deadlocks.</div>", (value.BlockedThreads.Count > 1) ? "these threads are" : "this thread is");
				value.Description += text;
			}
			else
			{
				if (value.Category.StartsWith("DOTNETWAITINGINWAIT") || value.Category.StartsWith("DOTNETWAITINGINTHREADSLEEP"))
				{
					value.IsError = false;
					value.IsWarning = false;
					value.Weight = -2;
				}
				if (value.BlockedThreads.Count < 2)
				{
					switch (value.Category)
					{
					case "DOTNETTHREADINGMONITORWAIT":
					case "DOTNETWAITINGTOENTERLOCK":
					case "DOTNETWAITINGINTHREADSLEEP":
					case "SLEEP":
						value.IsError = false;
						value.IsWarning = false;
						break;
					}
				}
			}
			if (!flag)
			{
				if (value.IsError)
				{
					Globals.Manager.ReportError(value.getBlockingReport(), value.Recommendation, value.Weight, "{40710b7a-a13e-48ae-b123-f9ed7eb49d04}");
				}
				else if (value.IsWarning)
				{
					Globals.Manager.ReportWarning(value.getBlockingReport(), value.Recommendation, value.Weight, "{fcadff06-4455-4c0c-b460-ae8d3839599e}");
				}
				else
				{
					Globals.Manager.ReportInformation(value.getBlockingReport(), value.Weight, "{6f016754-1e91-416b-9ced-30d897c54157}");
				}
			}
			Globals.HelperFunctions.IncrementSubStatus();
		}
		Globals.HelperFunctions.ResetStatus("Reporting detailed thread information", Globals.g_AnalyzedThreads.ItemsCount, "Thread");
		ReportSection currentSection = Globals.Manager.CurrentSection;
		ThreadSummaryReportSection val = new ThreadSummaryReportSection("ThreadReport", Globals.Manager);
		currentSection.AddChildSection((ReportSection)(object)val);
		((ReportSection)val).Title = "Thread Report";
		((ReportSection)val).Collapsible = true;
		Globals.Manager.CurrentSection = (ReportSection)(object)val;
		foreach (AnalyzedThreadClass item in Globals.g_AnalyzedThreads.Items)
		{
			ReportThread(item);
			Globals.HelperFunctions.IncrementSubStatus();
		}
		Globals.Manager.CurrentSection = currentSection;
	}

	public object GetCOMCreationAdditionalInfo(CacheFunctions.ScriptStackFrameClass StackFrame, string FunctionName)
	{
		object obj = null;
		obj = "This thread is calling <b>";
		switch (FunctionName)
		{
		case "OLE32!COCREATEINSTANCE":
		case "COMBASE!COCREATEINSTANCE":
			obj = obj.ToString() + "CoCreateInstance ";
			break;
		case "OLE32!COCREATEINSTANCEEX":
		case "COMBASE!COCREATEINSTANCEEX":
			obj = obj.ToString() + "CoCreateInstanceEx ";
			break;
		case "ASP!CSERVER::CREATEOBJECT":
			obj = obj.ToString() + "Server.CreateObject in ASP ";
			break;
		case "MSVBVM60!RTCCREATEOBJECT2":
		case "VBSCRIPT!GETOBJECTFROMPROGID":
			obj = obj.ToString() + "CreateObject ";
			break;
		}
		obj = obj.ToString() + "</b> to create a component ";
		switch (FunctionName)
		{
		case "OLE32!COCREATEINSTANCE":
		case "OLE32!COCREATEINSTANCEEX":
		case "COMBASE!COCREATEINSTANCE":
		case "COMBASE!COCREATEINSTANCEEX":
			if (Globals.g_OSPlatformVersion == "X86")
			{
				obj = obj.ToString() + "with CLSID = <font color=blue>\"" + Globals.HelperFunctions.GetGUIDString(StackFrame.Args(0).ToString()) + "\"</font><br><br>";
			}
			else if (Globals.g_OSPlatformVersion == "X64")
			{
				obj = obj.ToString() + "with CLSID = <font color=blue>\"" + Globals.HelperFunctions.GetGUIDString(Globals.HelperFunctions.GetQwordAtSymbolNoErrors("0n" + Convert.ToString(StackFrame.ChildEBP - 48.0))) + "\"</font><br><br>";
			}
			break;
		case "ASP!CSERVER::CREATEOBJECT":
		case "MSVBVM60!RTCCREATEOBJECT2":
		case "VBSCRIPT!GETOBJECTFROMPROGID":
			if (Globals.g_OSPlatformVersion == "X86")
			{
				obj = obj.ToString() + "named <font color=blue>\"" + Globals.g_Debugger.ReadUnicodeString(StackFrame.Args(1)) + "\"</font><br><br>";
			}
			else if (Globals.g_OSPlatformVersion == "X64")
			{
				obj = obj.ToString() + "named <font color=blue>\"" + Globals.g_Debugger.ReadUnicodeString(Convert.ToDouble(Globals.HelperFunctions.GetQwordAtSymbolNoErrors("0n" + StackFrame.ChildEBP))) + "\"</font><br><br>";
			}
			break;
		}
		return obj;
	}

	public string GetCOMCreationRecommendation(CacheFunctions.ScriptThreadClass Thread, string FunctionName)
	{
		StringBuilder stringBuilder = null;
		string text = null;
		stringBuilder = new StringBuilder();
		stringBuilder.Append("The thread(s) in question is/are waiting on a ");
		switch (FunctionName)
		{
		case "OLE32!COCREATEINSTANCE":
		case "OLE32!COCREATEINSTANCEEX":
		case "COMBASE!COCREATEINSTANCE":
		case "COMBASE!COCREATEINSTANCEEX":
			stringBuilder.Append("CoCreateInstance call ");
			break;
		case "ASP!CSERVER::CREATEOBJECT":
			stringBuilder.Append("Server.CreateObject call ");
			break;
		case "MSVBVM60!RTCCREATEOBJECT2":
		case "VBSCRIPT!GETOBJECTFROMPROGID":
			stringBuilder.Append("CreateObject call ");
			break;
		}
		text = Globals.HelperFunctions.GetThreadIDWithLink(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(Thread.COMDestinationThreadID));
		if (Thread.COMDestinationProcessID == Globals.g_Debugger.ProcessID)
		{
			stringBuilder.Append("being handled by thread " + text + " ");
		}
		stringBuilder.Append("to return. Further analysis of <b>");
		if (Thread.COMDestinationProcessID == Globals.g_Debugger.ProcessID)
		{
			stringBuilder.Append(text);
		}
		else
		{
			stringBuilder.Append("the process hosting the particular component (<font color=red>not</font> the RpcSs service)");
		}
		stringBuilder.Append("</b> should be performed to determine why these calls have not completed. More information for the particular component(s) that the thread(s) in question is/are attempting to instantiate can be found in the thread detail for each thread listed in the Description pane to the left.");
		return stringBuilder.ToString();
	}

	public AnalyzedThreadClass getComCallAnalysis(CacheFunctions.ScriptThreadClass Thread)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = null;
		StringBuilder stringBuilder = null;
		AnalyzedThreadClass analyzedThreadClass2 = null;
		string text = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		StringBuilder stringBuilder2 = null;
		string text2 = null;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		NetDbgObj val = null;
		val = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		stringBuilder = new StringBuilder();
		stringBuilder2 = new StringBuilder();
		int cOMDestinationProcessID = Thread.COMDestinationProcessID;
		analyzedThreadClass.KeyPartOne = cOMDestinationProcessID.ToString();
		int cOMDestinationThreadID = Thread.COMDestinationThreadID;
		analyzedThreadClass.KeyPartTwo = cOMDestinationThreadID.ToString();
		analyzedThreadClass.IsWarning = true;
		if (Thread.FindFrameInStack("WAMREG!CWAMADMIN::APPUNLOAD") > -1)
		{
			stringBuilder2.Append("This thread is in the process of attempting to shut down the following application:<br><br><b>" + Globals.g_Debugger.ReadUnicodeString(scriptStackFrameClass.Args(1)) + "</b> (running in process <b>" + Thread.COMDestinationProcessID + "</b>)<br><br>If there are any pending requests to this application, further analysis of the dump file for process <b>" + Thread.COMDestinationProcessID + "</b> should be done.");
		}
		else if (Thread.FindFrameInStack("ASP!WAM_EXEC_INFO::SENDENTIRERESPONSEOOP") > -1)
		{
			analyzedThreadClass.Category = "OK";
		}
		else if (Thread.FindFrameInStack("W3ISAPI!GETSERVERVARIABLE") > -1)
		{
			analyzedThreadClass.Category = "OK";
		}
		else
		{
			flag4 = true;
		}
		int num = Thread.FindFrameInStack(Globals.g_COMRuntimeModule + "!COCREATEINSTANCEEX");
		if (num == -1)
		{
			num = Thread.FindFrameInStack(Globals.g_COMRuntimeModule + "!COCREATEINSTANCE");
			if (num == -1)
			{
				num = Thread.FindFrameInStack(Globals.g_COMRuntimeModule + "!COGETCLASSOBJECT");
			}
		}
		int num2 = Thread.FindFrameInStack("ASP!CSERVER::CREATEOBJECT");
		if (num2 == -1)
		{
			num2 = Thread.FindFrameInStack("MSVBVM60!RTCCREATEOBJECT2");
			if (num2 == -1)
			{
				num2 = Thread.FindFrameInStack("VBSCRIPT!GETOBJECTFROMPROGID");
			}
		}
		if (num > -1 || num2 > -1)
		{
			flag5 = true;
			if (num2 > -1)
			{
				num = num2;
			}
			if (cOMDestinationThreadID > 0)
			{
				flag6 = true;
			}
			else
			{
				int num3 = Thread.FindFrameInStack(Globals.g_COMRuntimeModule + "!CSTDMARSHAL::BEGIN_REMQIANDUNMARSHAL1");
				if (num3 > -1 && num3 < num)
				{
					flag6 = true;
				}
			}
			scriptStackFrameClass = Thread.StackFrames[num];
			text = CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress);
			analyzedThreadClass.AdditionalInfo = GetCOMCreationAdditionalInfo(scriptStackFrameClass, text).ToString();
			flag4 = flag6 || Globals.g_Debugger.ProcessID == Thread.COMDestinationProcessID;
			if (!flag4)
			{
				stringBuilder2.Append(GetCOMCreationRecommendation(Thread, text));
			}
		}
		if (flag5 && !flag6)
		{
			analyzedThreadClass.Category = "COMCALLACTIVATION";
			if (Globals.g_Debugger.ProcessID == cOMDestinationProcessID)
			{
				stringBuilder.Append("making a COM call");
				stringBuilder.Append(" to create a COM component ");
				stringBuilder.Append("within the same process");
			}
			else
			{
				stringBuilder.Append("waiting on the local <b>RpcSs service</b> to complete a COM activation request in <b>another COM server process</b>.");
			}
		}
		else
		{
			stringBuilder.Append("making a COM call");
			switch (cOMDestinationThreadID)
			{
			case 0:
				stringBuilder.Append(" to <b>multi-threaded apartment (MTA)</b> ");
				analyzedThreadClass.Category = "COMCALL";
				text2 = "MTA";
				break;
			case -1:
				stringBuilder.Append(" to <b>neutral apartment (NA)</b> ");
				analyzedThreadClass.Category = "COMCALL";
				text2 = "NA";
				break;
			default:
				if (Globals.g_Debugger.ProcessID == cOMDestinationProcessID)
				{
					stringBuilder.Append(" to thread " + Globals.HelperFunctions.GetThreadIDWithLink(Globals.HelperFunctions.GetLogicalThreadNumFromSystemTID(cOMDestinationThreadID)) + " ");
				}
				else if (val != null)
				{
					stringBuilder.Append(" to " + Globals.HelperFunctions.GetThreadAndProcessIDWithLinkOOP(val, cOMDestinationThreadID) + " ");
					Globals.HelperFunctions.CloseDebuggerForPid(cOMDestinationProcessID);
				}
				else
				{
					stringBuilder.Append(" to <b>thread with system id " + cOMDestinationThreadID + " </b> ");
				}
				analyzedThreadClass.Category = "COMCALLSTA";
				text2 = "STA";
				break;
			}
			if (val == null)
			{
				if (Globals.g_Debugger.ProcessID == cOMDestinationProcessID)
				{
					stringBuilder.Append("within the same process");
				}
				else
				{
					stringBuilder.Append("in <b>process " + cOMDestinationProcessID + "</b>");
				}
			}
			val = null;
			if (flag6)
			{
				stringBuilder.Append(" to unmarshal a newly-created COM component");
			}
		}
		if (flag4)
		{
			if (stringBuilder2.ToString() != "")
			{
				stringBuilder2.Append("<br><br>");
			}
			if (text2 == "STA")
			{
				stringBuilder2.Append("Several threads making calls to the same STA thread can cause a performance bottleneck due to serialization.");
				int num4 = -1;
				if (cOMDestinationThreadID == Globals.AnalyzeComPlus.GetHostApartmentID("MAIN"))
				{
					flag3 = true;
					num4++;
				}
				if (cOMDestinationThreadID == Globals.AnalyzeComPlus.GetHostApartmentID("AT"))
				{
					flag2 = true;
					num4++;
				}
				if (cOMDestinationThreadID == Globals.AnalyzeComPlus.GetHostApartmentID("STMT"))
				{
					flag = true;
					num4++;
				}
				if (num4 == -1)
				{
					stringBuilder2.Append(" Server side COM servers are recommended to be thread aware and follow MTA guidelines when multiple threads are sharing the same object instance.");
				}
				else
				{
					stringBuilder2.Append("<br><br>Note:  This STA thread is one of the special-purposed STA threads in the process.  This particular STA thread is used to");
					if (num4 > 0 && flag3 && num > 0 && Globals.AnalyzeThreads.IsThreadSTA(Thread))
					{
						flag2 = false;
						flag = false;
						num4 = 0;
					}
					if (num4 == 0)
					{
						if (flag2)
						{
							stringBuilder2.Append(" " + Globals.HelperFunctions.GetSpecialSTABlurb("AT") + ". ");
						}
						else if (flag)
						{
							stringBuilder2.Append(" " + Globals.HelperFunctions.GetSpecialSTABlurb("STMT") + ". ");
						}
						else if (flag3)
						{
							stringBuilder2.Append(" " + Globals.HelperFunctions.GetSpecialSTABlurb("MAIN") + ". ");
						}
					}
					else
					{
						stringBuilder2.Append(":<br>");
						if (flag3)
						{
							stringBuilder2.Append(Globals.HelperFunctions.Spaces(2) + "- " + Globals.HelperFunctions.GetSpecialSTABlurb("MAIN") + "<br>");
						}
						if (flag2)
						{
							stringBuilder2.Append(Globals.HelperFunctions.Spaces(2) + "- " + Globals.HelperFunctions.GetSpecialSTABlurb("AT") + "<br>");
						}
						if (flag)
						{
							stringBuilder2.Append(Globals.HelperFunctions.Spaces(2) + "- " + Globals.HelperFunctions.GetSpecialSTABlurb("STMT") + "<br>");
						}
						stringBuilder2.Append("<br>");
					}
					if (flag3)
					{
						stringBuilder2.Append("To avoid serializing multiple concurrent calls to the Main STA thread, change the ThreadingModel of the COM component from 'Single' to something compatible with the apartment type of the client thread (i.e. 'Both', 'Neutral', or 'Apartment' for STA clients, or 'Both', 'Neutral', or 'Free' for MTA clients.");
					}
					if (flag2 || flag)
					{
						if (flag3)
						{
							stringBuilder2.Append("<br><br>");
						}
						stringBuilder2.Append("To avoid serializing multiple concurrent calls to a Host STA thread, possible solutions include (in order of preference):<br>" + Globals.HelperFunctions.Spaces(2) + "1.  Change the ThreadingModel of the COM component from 'Apartment' to 'Both', if possible<br>" + Globals.HelperFunctions.Spaces(2) + "2.  Change client thread type to STA, if possible<br>" + Globals.HelperFunctions.Spaces(5) + "-  For ASP.NET clients, use AspCompat = 'true' (<a href = 'http://msdn.microsoft.com/library/default.aspx?url=/library/en-us/cpguide/html/cpconCOMComponentCompatibility.asp'>more info</a>)<br>" + Globals.HelperFunctions.Spaces(5) + "-  For ASP clients, use AspExecuteInMTA = 0 (<a href = 'http://www.microsoft.com/technet/prodtechnol/WindowsServer2003/Library/IIS/e9880a74-49bd-4fb6-9856-c4f4c5108207.mspx'>more info</a>)<br>" + Globals.HelperFunctions.Spaces(5) + "-  For custom clients, use COINIT_APARTMENTTHREADED instead of COINIT_MULTITHREADED when calling CoInitializeEx (<a href = 'http://msdn.microsoft.com/library/default.aspx?url=/library/en-us/com/html/0ac4a809-05f8-46d7-8e79-9d4e88b487f4.asp'>more info</a>)<br>" + Globals.HelperFunctions.Spaces(2) + "3.  Install the COM component in a COM+ application (library or server).<br>" + Globals.HelperFunctions.Spaces(5) + "-  For ASP.NET WebServices clients calling Visual Basic 6 components, this is the only option (<a href = 'http://support.microsoft.com/default.aspx?scid=KB;EN-US;303375'>more info</a>)<br>" + Globals.HelperFunctions.Spaces(5) + "-  For BizTalk Application Integration Components (AICs) that are written in Visual Basic 6, this is the only option (<a href = 'http://support.microsoft.com/default.aspx?scid=kb;EN-US;327323'>more info</a>)");
					}
				}
			}
			else
			{
				stringBuilder2.Append("Several threads making calls to the " + text2 + " of the same process could indicate a problem with the downstream threads which are processing those requests.  Further analysis of those downstream threads ");
				if (Globals.g_Debugger.ProcessID == cOMDestinationProcessID)
				{
					stringBuilder2.Append("within the same process ");
				}
				else
				{
					stringBuilder2.Append("in <b>process " + cOMDestinationProcessID + "</b> ");
				}
				stringBuilder2.Append("should be performed to ensure that there is no underlying problem.");
				if (Globals.g_Debugger.ProcessID != cOMDestinationProcessID)
				{
					stringBuilder2.Append(" ProcessID " + cOMDestinationProcessID + " is ");
					if (!Globals.HelperFunctions.ProcessIsIncludedInThisReport(cOMDestinationProcessID))
					{
						stringBuilder2.Append("not ");
					}
					stringBuilder2.Append("included in this analysis report.");
				}
			}
		}
		analyzedThreadClass.Recommendation = stringBuilder2.ToString();
		analyzedThreadClass.Description = stringBuilder.ToString();
		if (cOMDestinationThreadID > 0 && Globals.g_Debugger.ProcessID == cOMDestinationProcessID)
		{
			scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(cOMDestinationThreadID);
			if (scriptThreadClass != null)
			{
				if (Globals.g_AnalyzedThreads.Exists(scriptThreadClass.ThreadID))
				{
					analyzedThreadClass2 = Globals.g_AnalyzedThreads.Item(scriptThreadClass.ThreadID);
				}
				else
				{
					if (!Globals.g_AnalyzedThreads.Exists(Thread.ThreadID))
					{
						Globals.g_AnalyzedThreads.Add(analyzedThreadClass);
					}
					analyzedThreadClass2 = getAnalysis(scriptThreadClass);
					if (!Globals.g_AnalyzedThreads.Exists(scriptThreadClass.ThreadID))
					{
						Globals.g_AnalyzedThreads.Add(analyzedThreadClass2);
					}
				}
				if (analyzedThreadClass2.Category != "OK")
				{
					stringBuilder.Append(" which in turn is " + analyzedThreadClass2.Description);
					analyzedThreadClass.Description = stringBuilder.ToString();
				}
				else if (analyzedThreadClass2.Description != null && analyzedThreadClass2.Description.StartsWith("waiting on critical section"))
				{
					stringBuilder.Append(" which in turn is " + analyzedThreadClass2.Description);
					analyzedThreadClass.Description = stringBuilder.ToString();
				}
			}
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getCritSecAnalysis(CacheFunctions.ScriptThreadClass Thread)
	{
		StringBuilder stringBuilder = null;
		IDbgCritSec val = null;
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		stringBuilder = new StringBuilder();
		analyzedThreadClass.Category = "OK";
		val = Globals.g_Debugger.CritSecs.GetCritSecByAddress(Thread.WaitingOnCritSecAddr);
		stringBuilder.Append("waiting on critical section " + Globals.HelperFunctions.GetCritSecWithLink(val.Address.ToString()));
		switch (val.State.ToUpper())
		{
		case "LOCKED":
			stringBuilder.Append(" which is owned by thread " + Globals.HelperFunctions.GetThreadIDWithLink(val.OwnerThreadID) + "<br>");
			break;
		case "TRANSITIONING":
			stringBuilder.Append(" which is <b>transitioning owners</b> (no current owner)<br>");
			break;
		case "DEADLOCKED":
			stringBuilder.Append(" owned by thread " + Globals.HelperFunctions.GetThreadIDWithLink(val.OwnerThreadID) + ".<br>");
			stringBuilder.Append("Thread " + Globals.HelperFunctions.GetThreadIDWithLink(val.OwnerThreadID) + " in turn is <b>deadlocked</b> with another thread.<br>");
			break;
		case "ORPHANED":
			stringBuilder.Append(" which has been orphaned.<br>");
			break;
		case "UNINITIALIZED":
			stringBuilder.Append(" which is <b>uninitialized</b><br>");
			break;
		}
		analyzedThreadClass.Description = stringBuilder.ToString();
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getMsgBoxAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID, string FunctionName)
	{
		double num = 0.0;
		AnalyzedThreadClass analyzedThreadClass = null;
		string text = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		scriptStackFrameClass = Thread.StackFrames[FrameID];
		switch (FunctionName)
		{
		case "USER32!MESSAGEBOXEXW":
			text = Globals.g_Debugger.ReadUnicodeString(scriptStackFrameClass.Args(1));
			break;
		case "USER32!MESSAGEBOXEXA":
			text = Globals.g_Debugger.ReadANSIString(scriptStackFrameClass.Args(1));
			break;
		case "USER32!MESSAGEBOXINDIRECTW":
		{
			double num2 = Globals.g_Debugger.ReadDWord(scriptStackFrameClass.Args(0));
			num2 = Globals.g_Debugger.ReadDWord(num2 + 12.0);
			text = Globals.g_Debugger.ReadUnicodeString(num2);
			break;
		}
		case "MSVBVM60!VBMESSAGEBOX2":
			text = Globals.g_Debugger.ReadANSIString(scriptStackFrameClass.Args(1));
			break;
		}
		analyzedThreadClass.Category = "MESSAGEBOX";
		analyzedThreadClass.IsError = true;
		num = ((!(FunctionName == "MSVBVM60!VBMESSAGEBOX2")) ? Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "USER32!MESSAGEBOX", FrameID) : Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "MSVBVM60!", FrameID));
		analyzedThreadClass.Description = "displaying a <b>message box</b>. The call to display the message box originated from <b>" + Globals.g_Debugger.GetSymbolFromAddress(num) + "</b>.";
		if (text != null)
		{
			AnalyzedThreadClass analyzedThreadClass2 = analyzedThreadClass;
			analyzedThreadClass2.Description = analyzedThreadClass2.Description + "<br><br>The text of the message being displayed is:<div class='summaryItemCallout'>" + text.ToString() + "</div>";
		}
		analyzedThreadClass.Recommendation = "Server-side applications should not have any UI elements since they are supposed to run without any user intervention. Moreover, service applications run in non-interactive desktops, so no one can actually see the message box and dismiss it. This causes the application to hang." + Globals.HelperFunctions.GetVendorMessage(num);
		analyzedThreadClass.KeyPartOne = num.ToString();
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getASPDebugAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "ASPAPPDEBUG",
			IsError = true,
			Description = "hung due to ASP server side script debugging being enabled for one or more applications",
			Recommendation = "Enabling server side ASP script debugging for an application can have an adverse affect on performance, as each request to that application will be serialized to a single thread, which can result in request queuing. Please see the recommendation pertaining to ASP server-side debugging for more information and a list of the ASP applications that server side debugging is enabled on."
		};
	}

	public AnalyzedThreadClass getLoadLibraryAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		analyzedThreadClass.Category = "LOADLIBRARY";
		analyzedThreadClass.IsWarning = true;
		double num = (ulong)Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "KERNEL32!LOADLIBRARY", FrameID);
		scriptStackFrameClass = Thread.StackFrames[FrameID];
		string text = Globals.g_Debugger.ReadUnicodeString(scriptStackFrameClass.Args(0));
		analyzedThreadClass.Description = "loading <b>" + text + "</b> using the API LoadLibrary. The call to LoadLibrary originated originated from <b>" + Globals.g_Debugger.GetSymbolFromAddress(num) + "</b>";
		analyzedThreadClass.Recommendation = "Constant calls to LoadLibrary and FreeLibrary can have a serious impact on application performance since the Windows NT Loader takes a global lock while performing this operation causing serialization." + Globals.HelperFunctions.GetVendorMessage(num);
		analyzedThreadClass.KeyPartOne = num.ToString();
		analyzedThreadClass.KeyPartTwo = Convert.ToString(text);
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getWriteClientAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass obj = new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "WRITECLIENT",
			IsWarning = true
		};
		double directCaller = Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "W3ISAPI!WRITECLIENT", FrameID);
		obj.Description = "returning data to the client using the WriteClient API, which can result in performance degredation on IIS 6.0 due to a known issue with the HTTP.SYS kernel mode driver.<br><br>The call to WriteClient originated from the following ISAPI: <b>" + Globals.g_Debugger.GetSymbolFromAddress(directCaller) + "</b>";
		obj.Recommendation = "There is a known performance issue in IIS 6.0 when using the WriteClient API to send data. KB article <b><a target='_blank' href=http://support.microsoft.com/?id=840875>Q840875</a></b> provides more information on this issue, along with a potential workaround.";
		obj.KeyPartOne = directCaller.ToString();
		return obj;
	}

	public AnalyzedThreadClass getCrlAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		analyzedThreadClass.Category = "CRL";
		analyzedThreadClass.IsWarning = true;
		analyzedThreadClass.Description = "checking the certificate revocation lists (CRLs) to verify that a certificate is still valid";
		analyzedThreadClass.Recommendation = "Ensure that the machine has outbound http connectivity to all required CRLs, or download and install the CRLs (see <a href='https://technet.microsoft.com/en-us/library/aa996972(v=exchg.65).aspx'>How to Manually Import a CRL</a>).<br><br>Note in some cases it may be possible to disable the CRL checks.  The specific steps to disable it will depend on the specific component that is making the check.  See the articles listed below for some examples: <div style='margin: 20px'><ul><li><a href='http://blogs.msdn.com/b/tess/archive/2008/05/13/asp-net-hang-authenticode-signed-assemblies.aspx'>ASP.NET Hang: Authenticode signed assemblies</a></li><li><a href='http://blogs.msdn.com/b/gregmcb/archive/2008/05/06/ssl-and-authenticode-causes-crl-lookups-if-your-machine-cannot-access-the-crl-for-verification.aspx'>SSL and Authenticode Causes CRL lookups if Your Machine Cannot Access the CRL for Verification</a></li><li><a href='http://blogs.msdn.com/b/tom/archive/2008/10/28/web-site-stops-responding-for-15-25-seconds.aspx'>Web Site Stops Responding for 15-25 seconds</a></li><li><a href='http://blogs.msdn.com/b/andreal/archive/2008/07/19/wcf-service-startup-too-slow-have-you-thought-to-crl-check.aspx'>WCF service startup too slow? Have you thought to CRL check?</a></li><li><a href='https://support.microsoft.com/en-us/kb/936707'>FIX: A .NET Framework 2.0 managed application that has an Authenticode signature takes longer than usual to start</a></li><li><a href='http://forums.iis.net/t/1100044.aspx'>Disable Certificate Revocation List RSS?</a></li></ul></div>";
		if (Thread.StackFrames.Count > 2 && Globals.g_OSPlatformVersion == "X64")
		{
			ulong num = (ulong)Thread.StackFrames[2].Args(2);
			if (num != 0L)
			{
				string text = Globals.g_Debugger.ReadUnicodeString(num);
				if (!string.IsNullOrEmpty(text) && text.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
				{
					AnalyzedThreadClass analyzedThreadClass2 = analyzedThreadClass;
					analyzedThreadClass2.Description = analyzedThreadClass2.Description + ".  The particular CRL being retrieved at the time of the dump is:<div class='summaryItemCallout'>" + text + "</div><i>(Note, there may be additional CRLs required as well)</i>";
					analyzedThreadClass.KeyPartOne = text;
					return analyzedThreadClass;
				}
			}
		}
		Thread.StringSearch("http", 0uL, 0uL);
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getIISIntrinsicsAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		bool bIsUnmarshaling = false;
		string text = null;
		analyzedThreadClass = null;
		if (Globals.HelperFunctions.IsIISIntrinsicsStack(Thread, out bIsUnmarshaling))
		{
			text = ((!bIsUnmarshaling) ? "M" : "Unm");
			analyzedThreadClass = new AnalyzedThreadClass();
			analyzedThreadClass.Thread = Thread;
			analyzedThreadClass.Category = "IISINTRINSICS";
			analyzedThreadClass.IsError = false;
			analyzedThreadClass.IsWarning = true;
			if (bIsUnmarshaling)
			{
				analyzedThreadClass.Description = "making a callback to IIS to unmarshal the <b>IIS Intrinsic Objects</b>";
			}
			else
			{
				analyzedThreadClass.Description = "making a callback to IIS to marshal the <b>IIS Intrinsic Objects</b>";
			}
			analyzedThreadClass.Recommendation = text + "arshaling of the IIS intrinsic objects commonly causes delays in the application due to delays at the network transport level.  Possible solutions to this problem include:<ul><li>Eliminate the potential for this type of delay by disabling the IISIntrinsics property on all COM+ components which do not require their use.  Note this is a best-practice since it reduces unnecessary application processing and network overhead.</li><li>Collect a network capture of the problem to investigate the network transport delay. Note retries due to failures at the network transport level can cause such delays.</li></ul>See the following Knowledge Base article for more information: <a target='_blank' href='http://support.microsoft.com/?id=287422'> IISIntrinsics flow by default when you call COM+ components from IIS 5.0 and later applications</a>";
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getSleepAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		string text = null;
		string text2 = null;
		int num = 0;
		bool flag = false;
		string text3 = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		analyzedThreadClass = null;
		dictionary = Thread.StackFrames;
		if (!flag && Globals.g_collCOMPlusSTAThreadPoolThreads.ContainsKey(Thread.ThreadID))
		{
			flag = true;
		}
		if (Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID) != null)
		{
			flag = true;
		}
		if (!flag && Globals.g_collSpecialSTAThreads.ContainsKey(Thread.ThreadID))
		{
			flag = true;
		}
		if (!flag)
		{
			for (num = 0; num < dictionary.Count; num++)
			{
				scriptStackFrameClass = dictionary[num];
				switch (CacheFunctions.GetFunctionName(scriptStackFrameClass.InstructionAddress).ToUpper())
				{
				case "OLE32!APPINVOKE":
				case "COMBASE!APPINVOKE":
				case "COMSVCS!CSTAACTIVITYWORK::DOWORK":
				case "COMSVCS!STAMESSAGEWORK::DOWORK":
					flag = true;
					break;
				default:
					scriptModuleClass = CacheFunctions.GetModuleFromAddress(scriptStackFrameClass.InstructionAddress);
					if (scriptModuleClass == null || (!scriptModuleClass.IsISAPIFilter && !scriptModuleClass.IsISAPIExtension) || !(scriptModuleClass.ModuleName.ToUpper() != "ASPNET_ISAPI"))
					{
						continue;
					}
					flag = true;
					break;
				}
				break;
			}
		}
		if (flag)
		{
			analyzedThreadClass = new AnalyzedThreadClass();
			analyzedThreadClass.Thread = Thread;
			scriptStackFrameClass = dictionary[FrameID];
			double num2 = scriptStackFrameClass.Args(0);
			analyzedThreadClass.Category = "SLEEP";
			analyzedThreadClass.IsWarning = true;
			double directCaller = Globals.HelperFunctions.GetDirectCaller(dictionary, "KERNEL32!SLEEP", FrameID);
			text2 = "calling the <b>Sleep</b> API. ";
			if (directCaller > 0.0)
			{
				text2 = text2.ToString() + "The call to this API originated from <b>" + Globals.g_Debugger.GetSymbolFromAddress(directCaller) + "</b>. ";
			}
			text = "The duration of the Sleep call is <b>";
			if (num2 < 1000.0)
			{
				text = text + num2 + " miliseconds</b>.  Short calls to the Sleep API often occur inside of a tight loop, which will delay the application and cause high CPU until the loop is exited.";
			}
			else
			{
				text = ((!(num2 < 60000.0)) ? (text + num2 / 60000.0 + " minutes</b>.  ") : (text + num2 / 1000.0 + " seconds</b>.  "));
				text += "Long calls to the Sleep API can delay the application significantly. ";
			}
			text += Globals.HelperFunctions.GetVendorMessage(directCaller);
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = text2;
			analyzedThreadClass.Recommendation = text;
			analyzedThreadClass.KeyPartOne = directCaller.ToString();
			analyzedThreadClass.KeyPartTwo = num2.ToString();
		}
		return analyzedThreadClass;
	}

	public void AppendAnalysis(AnalyzedThreadClass AnalyzedThread, AnalyzedThreadClass subAnalyzedThread, string InfoType)
	{
		if (subAnalyzedThread == null)
		{
			return;
		}
		if (AnalyzedThread.Category == "OK" && subAnalyzedThread.Category != "OK")
		{
			AnalyzedThreadClass analyzedThreadClass = AnalyzedThread;
			AnalyzedThread = subAnalyzedThread;
			subAnalyzedThread = analyzedThreadClass;
		}
		if (subAnalyzedThread == null)
		{
			return;
		}
		string additionalInfo = subAnalyzedThread.AdditionalInfo;
		string text;
		if (Convert.ToString(additionalInfo).GetSafeLength() != 0)
		{
			text = AnalyzedThread.AdditionalInfo;
			if (text.GetSafeLength() != 0 && !text.StartsWith("<BR><BR>"))
			{
				if (!text.StartsWith("<BR>"))
				{
					text = text.ToString() + "<br>";
				}
				text = text.ToString() + "<br>";
			}
			AnalyzedThread.AdditionalInfo = text + additionalInfo;
			if (InfoType != "")
			{
				AnalyzedThread.KeyPartOne = AnalyzedThread.KeyPartOne.ToString() + "+" + InfoType;
				AnalyzedThread.Recommendation = AnalyzedThread.Recommendation + "<br><br><b>Note</b> - additional " + InfoType + " information is also available in the thread details section of each thread listed in the Description pane to the left.";
			}
		}
		if (Convert.ToString(subAnalyzedThread.Description).GetSafeLength() <= 0 || (subAnalyzedThread.Category == "RPC" && AnalyzedThread.Category.Substring(0, 7) == "COMCALL" && (AnalyzedThread.Category.StartsWith("COMCALLACTIVATION") || subAnalyzedThread.Description.IndexOf("<b>LRPC</b>") >= 0)))
		{
			return;
		}
		text = AnalyzedThread.Description;
		if (Convert.ToString(text).GetSafeLength() > 0)
		{
			if (!text.StartsWith("<BR>"))
			{
				text = text.ToString() + "<br>";
			}
			text = text.ToString() + "<br>These threads are also ";
		}
		AnalyzedThread.Description = text.ToString() + subAnalyzedThread.Description;
	}

	public AnalyzedThreadClass GetCallRunningServerAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		double num = 0.0;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		dictionary = Thread.StackFrames;
		double num2 = Globals.HelperFunctions.FromHex(FrameID.ToString());
		analyzedThreadClass.Category = "CALLRUNNINGSERVER";
		analyzedThreadClass.IsWarning = true;
		if (num2 > -1.0)
		{
			double childEBP = dictionary[Convert.ToInt32(num2)].ChildEBP;
			if (Globals.g_OSVER >= Globals.OS_VER_WINXP)
			{
				if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
				{
					string text = "";
					string text2 = "~" + FrameID + "e dqp " + Globals.HelperFunctions.GetAs64BitHexString(childEBP) + " l100";
					string text3 = Globals.g_Debugger.Execute(text2);
					string value = "rpcss!CServerTableEntry::`vftable'";
					int num3 = text3.IndexOf(value);
					if (num3 > 36)
					{
						text = text3.Substring(num3 - 36, 8);
						text += text3.Substring(num3 - 36 + 9, 8);
						double num4 = Globals.HelperFunctions.FromHex(text);
						num = Globals.g_Debugger.ReadDWord(num4 + 68.0);
					}
				}
				else
				{
					double num4 = Globals.g_Debugger.ReadDWord(childEBP - 4.0);
					num = Globals.g_Debugger.ReadDWord(num4 + 48.0);
				}
			}
			else
			{
				double num5 = Globals.g_Debugger.ReadDWord(childEBP - 4.0);
				double num6 = Globals.g_Debugger.ReadDWord(num5 + 20.0);
				num = Globals.g_Debugger.ReadDWord(num6 + 108.0);
			}
			analyzedThreadClass.Description = "forwarding a call to a COM server in <b>Process " + num + "</b> on the local machine";
			analyzedThreadClass.Recommendation = "Several threads blocking on calls to the same destination process could indicate a problem in that process.  Further analysis of the threads servicing these calls in <b>Process " + num + "</b> should be performed to ensure that there are no underlying problems.";
			analyzedThreadClass.KeyPartOne = num.ToString();
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass GetTxCommitAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "TXCOMMIT",
			IsError = false,
			IsWarning = true,
			Description = "attempting to commit a DTC transaction.  Various issues can cause a transaction to commit slowly or to block until timeout.  Examples include:<ol><li>Delays internal to any Resource Manager (RM), such as a database server, participating in the transaction</li><li>Delays internal to any client application participating in the transaction</li><li>Delays in the network between the Transaction Coordinator (MSDTC), and any RM or client participating in the transaction</li></ol>",
			Recommendation = "Each scenario in the Description pane to the left would require different troubleshooting steps.  Examples include:<ol><li>Use appropriate profiling utilities (i.e. SQL Profiler for SQL Server) and/or use DebugDiag to capture and analyze Userdumps of the processes hosting the Resource Managers during the transaction</li><li>Use network utilities (i.e. Netmon) to capture and analyze network traffic between MSDTC and the RMs and clients</li><li>Use DebugDiag to capture and analyze Userdumps of the processes hosting the client applications during the transaction, to ensure that no issues are preventing the clients from successfully voting on the transaction</li></ol>"
		};
	}

	public AnalyzedThreadClass GetTxVoteAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "TXVOTE",
			IsError = false,
			IsWarning = true,
			Description = "attempting to vote on a DTC transaction.  This transaction will not be able to commit until this client application is able to complete its vote. Any other application will block during this time if it attempts to commit the transaction, and the transaction will eventually be aborted if the transaction timeout is exceeded (default is 60 seconds).",
			Recommendation = "Review the call stack for this thread and any recommendations in this report pertaining to this thread, in order to determine what may be blocking this vote from completing."
		};
	}

	public string TrimLongDescription(string LongDescription)
	{
		if (LongDescription == null)
		{
			throw new ArgumentNullException("LongDescription");
		}
		if (LongDescription == "")
		{
			return null;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("making a COM call", "making a COM call");
		dictionary.Add("waiting on the local <b>RpcSs service</b>", "waiting on the local <b>RpcSs service</b>");
		dictionary.Add("displaying a <b>message box</b>", "displaying a <b>message box</b>");
		dictionary.Add("hung due to ASP server side script debugging", "hung due to ASP server side script debugging");
		dictionary.Add("returning data to the client using the WriteClient API", "returning data to the client using the WriteClient API");
		dictionary.Add("making a database operation using ADO", "making a database operation using ADO");
		dictionary.Add("making a network using WinInet library", "making a network using WinInet library");
		dictionary.Add("making a network using WinHTTP library", "making a network using WinHTTP library");
		dictionary.Add("waiting on data to be returned from another server via WinSock", "waiting on data to be returned from another server via WinSock");
		dictionary.Add("waiting on a socket function to complete", "waiting on a socket function to complete");
		dictionary.Add("calling the <b>Sleep</b> API", "calling the <b>Sleep</b> API");
		dictionary.Add("checking the certificate revocation lists (CRLs) to verify that a certificate is still valid", "checking the certificate revocation lists (CRLs) to verify that a certificate is still valid");
		dictionary.Add("waiting on critical section", "waiting on a critical section");
		dictionary.Add("loading <b>", "loading a dll using the LoadLibrary API");
		dictionary.Add("not fully resolved and may or may not be a problem", "not fully resolved and may or may not be a problem.  Further analysis of this thread may be required");
		dictionary.Add("incomplete and also has/have an invalid Thread Environment Block pointer", "incomplete and also has an invalid Thread Environment Block pointer");
		foreach (string key in dictionary.Keys)
		{
			if (LongDescription.StartsWith(key))
			{
				return dictionary[key];
			}
		}
		return LongDescription;
	}

	public AnalyzedThreadClass getAnalysis(CacheFunctions.ScriptThreadClass Thread)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		CacheFunctions.ScriptStackFrameClass StackFrame = null;
		int nFrameNum = -1;
		object FunctionName = null;
		IDbgCritSec CritSec = null;
		object ClientConnection = null;
		object ASPRequest = null;
		bool bSkip = false;
		analyzedThreadClass = null;
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("KERNEL32!UNHANDLEDEXCEPTIONFILTER");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getExceptionFilterAnalysis(Thread, nFrameNum);
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("NTDLL!RTLDISPATCHEXCEPTION");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getExceptionFilterAnalysis(Thread, nFrameNum);
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("COMSVCS!COMSVCSEXCEPTIONFILTER");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getComSvcsExceptionFilterAnalysis(ref Thread, nFrameNum);
			}
		}
		if (analyzedThreadClass == null)
		{
			if (Thread.WaitingOnCritSecAddr != 0.0)
			{
				analyzedThreadClass = getCritSecAnalysis(Thread);
			}
			else if (Thread.COMDestinationProcessID != 0)
			{
				analyzedThreadClass = getComCallAnalysis(Thread);
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("USER32!MESSAGEBOXEXW");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getMsgBoxAnalysis(Thread, nFrameNum, "USER32!MESSAGEBOXEXW");
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("USER32!MESSAGEBOXEXA");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getMsgBoxAnalysis(Thread, nFrameNum, "USER32!MESSAGEBOXEXA");
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("USER32!MESSAGEBOXINDIRECTW");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getMsgBoxAnalysis(Thread, nFrameNum, "USER32!MESSAGEBOXINDIRECTW");
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("MSVBVM60!VBMESSAGEBOX2");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getMsgBoxAnalysis(Thread, nFrameNum, "MSVBVM60!VBMESSAGEBOX2");
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("KERNEL32!LOADLIBRARYEXW");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getLoadLibraryAnalysis(Thread, nFrameNum);
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("W3ISAPI!WRITECLIENT");
			if (nFrameNum > -1)
			{
				analyzedThreadClass = getWriteClientAnalysis(Thread, nFrameNum);
			}
		}
		if (analyzedThreadClass == null)
		{
			nFrameNum = Thread.FindFrameInStack("CERTGETCERTIFICATECHAIN");
			if (nFrameNum > -1)
			{
				nFrameNum = Thread.FindFrameInStack("CRYPTRETRIEVEOBJECTBYURLWITHTIMEOUT");
				if (nFrameNum > -1)
				{
					analyzedThreadClass = getCrlAnalysis(Thread, nFrameNum);
				}
			}
		}
		if (analyzedThreadClass == null)
		{
			getASPDebugAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum, ref ASPRequest);
		}
		if (analyzedThreadClass == null)
		{
			getASPDebugAnalysis2_(Thread, ref analyzedThreadClass, ref nFrameNum, ref ASPRequest);
		}
		if (analyzedThreadClass == null)
		{
			getIISIntrinsicsAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			getIISIntrinsicsAnalysis2_(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			getSleepAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			GetCallRunningServerAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			GetTxCommitAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		GetTxVoteAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		getAdoDbAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		if (analyzedThreadClass == null)
		{
			getDotNetAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			analyzedThreadClass = Globals.AnalyzeManaged.RandomKnownIssues(Thread, nFrameNum);
		}
		getSocketAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref StackFrame, ref nFrameNum, ref FunctionName);
		if (analyzedThreadClass == null)
		{
			getWinInetAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			getWinHttpAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		if (analyzedThreadClass == null)
		{
			analyzedThreadClass = getIsapiAnalysisIfThreadMatches(Thread, analyzedThreadClass);
		}
		if (analyzedThreadClass == null)
		{
			getXmlHTTPAnalysisIfThreadMatches(Thread, ref analyzedThreadClass, ref nFrameNum);
		}
		analyzedThreadClass = getRpcCallAnalysisIfThreadMatches(Thread, analyzedThreadClass);
		analyzedThreadClass = getOKUnresolvedAndBadTebAnalysis(Thread, analyzedThreadClass);
		AvoidDoubleReportingCritSecIssues(Thread, analyzedThreadClass, ref CritSec, ref bSkip);
		if (analyzedThreadClass.Weight == 0)
		{
			analyzedThreadClass.Weight = 1;
		}
		HandleAspReqestsAndClientConns(Thread, analyzedThreadClass, ref ClientConnection, ref ASPRequest);
		return analyzedThreadClass;
	}

	private static void HandleAspReqestsAndClientConns(CacheFunctions.ScriptThreadClass Thread, AnalyzedThreadClass AnalyzedThread, ref object ClientConnection, ref object ASPRequest)
	{
		ASPRequest = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
		ClientConnection = Globals.g_HTTPInfo.GetClientConnectionByThreadID(Thread.ThreadID);
		if (ASPRequest != null)
		{
			AnalyzedThread.BlockedASPCount = 1;
			AnalyzedThread.Weight = 2;
		}
		else if (ClientConnection != null)
		{
			AnalyzedThread.BlockedClientConnCount = 1;
			AnalyzedThread.Weight = 2;
		}
	}

	private void AvoidDoubleReportingCritSecIssues(CacheFunctions.ScriptThreadClass Thread, AnalyzedThreadClass AnalyzedThread, ref IDbgCritSec CritSec, ref bool bSkip)
	{
		CritSec = Globals.HelperFunctions.OwnsCritSec(Thread.ThreadID);
		if (CritSec != null && Globals.g_collKnownCSIssueFound.ContainsKey(CritSec.OwnerThreadID))
		{
			string category = AnalyzedThread.Category;
			if ((category == "COMCALL" || category == "COMCALLSTA") && Globals.g_MixedComcallCritsecDeadlockDetected)
			{
				bSkip = true;
			}
			if (!bSkip)
			{
				AnalyzedThread.Category = "OK";
			}
		}
	}

	private AnalyzedThreadClass getOKUnresolvedAndBadTebAnalysis(CacheFunctions.ScriptThreadClass Thread, AnalyzedThreadClass AnalyzedThread)
	{
		if (AnalyzedThread == null)
		{
			AnalyzedThread = ((!Thread.HasValidTeb && Globals.g_Debugger.DumpType != "MINIDUMP") ? getBadTebAnalysisIfThreadMatches(Thread) : (Thread.HasAllGoodSymbols ? getOkAnalysisIfThreadMatches(Thread) : getUnresolvedAnalysisIfThreadMatches(Thread)));
		}
		else if (AnalyzedThread.Category == "OK")
		{
			if (!Thread.HasValidTeb)
			{
				AnalyzedThread = getBadTebAnalysisIfThreadMatches(Thread);
			}
			else if (!Thread.HasAllGoodSymbols)
			{
				AnalyzedThread = getUnresolvedAnalysisIfThreadMatches(Thread);
			}
		}
		return AnalyzedThread;
	}

	private void getSocketAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref CacheFunctions.ScriptStackFrameClass StackFrame, ref int nFrameNum, ref object FunctionName)
	{
		if (Globals.g_UtilExt.OSPlatformVersion == "X64")
		{
			nFrameNum = Thread.FindFrameInStack("NTDLL!NTWAITFORSINGLEOBJECT");
			if (nFrameNum == -1)
			{
				nFrameNum = Thread.FindFrameInStack("NTDLL!ZWWAITFORSINGLEOBJECT");
			}
			if (nFrameNum != -1)
			{
				StackFrame = Thread.StackFrames[nFrameNum];
				FunctionName = CacheFunctions.GetFunctionName(StackFrame.InstructionAddress);
				if (FunctionName.ToString().Substring(0, 7) != "MSWSOCK")
				{
					nFrameNum = -1;
				}
			}
		}
		else
		{
			nFrameNum = Thread.FindFrameInStack("MSAFD!SOCKWAITFORSINGLEOBJECT");
			if (nFrameNum == -1)
			{
				nFrameNum = Thread.FindFrameInStack("MSWSOCK!SOCKWAITFORSINGLEOBJECT");
			}
		}
		if (nFrameNum > -1)
		{
			if (AnalyzedThread == null)
			{
				AnalyzedThread = getSocketAnalysis(Thread, nFrameNum);
			}
			else
			{
				AppendAnalysis(AnalyzedThread, getSocketAnalysis(Thread, nFrameNum), "socket");
			}
		}
	}

	private static void getDotNetAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		if (Globals.g_clrModule != null)
		{
			nFrameNum = Thread.FindFrameInStack(Globals.g_clrModule.ModuleName.ToUpper() + "!");
			if (nFrameNum > -1)
			{
				AnalyzedThread = Globals.AnalyzeManaged.getDotNetAnalysis(Thread, nFrameNum);
			}
			else if (Convert.ToString(Thread.ClrStackReportNoArgs).GetSafeLength() != 0)
			{
				AnalyzedThread = Globals.AnalyzeManaged.getDotNetAnalysis(Thread, nFrameNum);
			}
		}
	}

	private void getAdoDbAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("MSADO15!");
		if (nFrameNum > -1)
		{
			if (AnalyzedThread == null)
			{
				AnalyzedThread = getAdoDbAnalysis(Thread, nFrameNum);
			}
			else
			{
				AppendAnalysis(AnalyzedThread, getAdoDbAnalysis(Thread, nFrameNum), "ADO");
			}
		}
	}

	private void GetTxVoteAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("COMSVCS!CTRANSACTIONVOTER::ONCALL");
		if (nFrameNum > -1)
		{
			if (AnalyzedThread == null)
			{
				AnalyzedThread = GetTxVoteAnalysis(Thread, nFrameNum);
			}
			else
			{
				AppendAnalysis(AnalyzedThread, GetTxVoteAnalysis(Thread, nFrameNum), "dtc");
			}
		}
	}

	private AnalyzedThreadClass getRpcCallAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, AnalyzedThreadClass AnalyzedThread)
	{
		if (Thread.RpcDestinationBindings != "" || Thread.RpcSourceBindings != "")
		{
			if (AnalyzedThread == null)
			{
				AnalyzedThread = getRpcCallAnalysis(Thread);
			}
			else
			{
				AppendAnalysis(AnalyzedThread, getRpcCallAnalysis(Thread), "RPC call");
			}
		}
		return AnalyzedThread;
	}

	private void getXmlHTTPAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("CXMLHTTP");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getXmlHTTPAnalysis(Thread, nFrameNum);
		}
	}

	private AnalyzedThreadClass getIsapiAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, AnalyzedThreadClass AnalyzedThread)
	{
		if (Thread.FirstISAPIDLL != null && Thread.FirstISAPIDLL.ModuleName.ToUpper() != "ASP" && Thread.FirstISAPIDLL.ModuleName.ToUpper() != "ASPNET_ISAPI")
		{
			AnalyzedThread = getIsapiAnalysis(Thread, Thread.FirstISAPIDLL);
		}
		return AnalyzedThread;
	}

	private void getWinHttpAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("WINHTTP!");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getWinHttpAnalysis(Thread, nFrameNum);
		}
	}

	private void getWinInetAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("WININET!");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getWinInetAnalysis(Thread, nFrameNum);
		}
	}

	private void getASPDebugAnalysis2_(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum, ref object ASPRequest)
	{
		if (!Globals.g_IsDebugEnabled)
		{
			return;
		}
		nFrameNum = Thread.FindFrameInStack("PDM!DLLUNREGISTERSERVER");
		if (nFrameNum > -1)
		{
			ASPRequest = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
			if (ASPRequest != null)
			{
				AnalyzedThread = getASPDebugAnalysis(Thread, nFrameNum);
			}
		}
	}

	private void getASPDebugAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum, ref object ASPRequest)
	{
		if (!Globals.g_IsDebugEnabled)
		{
			return;
		}
		nFrameNum = Thread.FindFrameInStack("PDM!CAPPLICATIONTHREAD::SUSPENDFORBREAKPOINT");
		if (nFrameNum > -1)
		{
			ASPRequest = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
			if (ASPRequest != null)
			{
				AnalyzedThread = getASPDebugAnalysis(Thread, nFrameNum);
			}
		}
	}

	private void GetTxCommitAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("MSDTCPRX!CITRANSACTION::COMMIT");
		if (nFrameNum > -1)
		{
			AnalyzedThread = GetTxCommitAnalysis(Thread, nFrameNum);
		}
	}

	private void GetCallRunningServerAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("RPCSS!CSERVERTABLEENTRY::CALLRUNNINGSERVER");
		if (nFrameNum > -1)
		{
			AnalyzedThread = GetCallRunningServerAnalysis(Thread, nFrameNum);
		}
	}

	private void getSleepAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("KERNEL32!SLEEPEX");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getSleepAnalysis(Thread, nFrameNum);
			return;
		}
		nFrameNum = Thread.FindFrameInStack("KERNEL32!SLEEP");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getSleepAnalysis(Thread, nFrameNum);
		}
	}

	private void getIISIntrinsicsAnalysis2_(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("COMSVCS!VARIANTUNMARSHAL");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getIISIntrinsicsAnalysis(Thread, nFrameNum);
		}
	}

	private void getIISIntrinsicsAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread, ref AnalyzedThreadClass AnalyzedThread, ref int nFrameNum)
	{
		nFrameNum = Thread.FindFrameInStack("COMSVCS!VARIANTMARSHAL");
		if (nFrameNum > -1)
		{
			AnalyzedThread = getIISIntrinsicsAnalysis(Thread, nFrameNum);
		}
	}

	public AnalyzedThreadClass getOkAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "OK"
		};
	}

	public AnalyzedThreadClass getUnresolvedAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		analyzedThreadClass.ClientConnInfo = Globals.g_HTTPInfo.GetClientConnectionByThreadID(Thread.ThreadID);
		analyzedThreadClass.ASPRequestInfo = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
		analyzedThreadClass.Recommendation = "Debug symbols for modules on these threads are either incorrect or missing which is preventing DebugDiag from completely resolving the callstacks. Ensure that a symbol path is configured, enabling the proper symbols to be loaded.<br><br>To modify or view the current symbol path from within DebugDiag, select <b>Symbol File Path</b> from the <b>File</b> menu. If symbols aren't available and/or a proper symbol path is already setup, then these threads should be manually reviewed further to determine what they are doing. For more information regarding debug symbols for Microsoft components, please visit the following site:<br><br><a target=_new href='http://www.microsoft.com/whdc/devtools/debugging/debugstart.mspx'>Debugging Tools and Symbols: Getting Started</a><br>";
		if (analyzedThreadClass.ClientConnInfo != null || analyzedThreadClass.ASPRequestInfo != null)
		{
			analyzedThreadClass.Description = "processing a client request and is/are not fully resolved. Further analysis of these threads is recommended in order to determine what may be blocking the request(s).";
			analyzedThreadClass.Category = "UNRESOLVED";
		}
		else
		{
			analyzedThreadClass.Description = "not fully resolved and may or may not be a problem. Further analysis of these threads may be required.";
			analyzedThreadClass.Category = "OK";
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getBadTebAnalysisIfThreadMatches(CacheFunctions.ScriptThreadClass Thread)
	{
		return new AnalyzedThreadClass
		{
			Thread = Thread,
			Category = "BADTEB",
			IsWarning = true,
			Description = "incomplete and also has/have an invalid Thread Environment Block pointer. As a result, the information reported is most likely inaccurate.",
			Recommendation = "If call stacks relevant to the problem could not be accurately analyzed due to bad TEB information then a new dump may need to be obtained."
		};
	}

	public AnalyzedThreadClass getRpcCallAnalysis(CacheFunctions.ScriptThreadClass Thread)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		string text6 = null;
		string text7 = null;
		string text8 = null;
		string[] array = null;
		string[] array2 = null;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		string text9 = null;
		string text10 = null;
		StringBuilder stringBuilder = null;
		StringBuilder stringBuilder2 = null;
		stringBuilder = new StringBuilder();
		stringBuilder2 = new StringBuilder();
		StringBuilder stringBuilder3 = null;
		stringBuilder3 = new StringBuilder();
		text8 = Thread.RpcDestinationBindings;
		text7 = Thread.RpcSourceBindings;
		if (text8 == "" && text7 == "")
		{
			return null;
		}
		array2 = text8.Split(';');
		array = text7.Split(';');
		num2 = array2.GetUpperBound(0);
		num = array.GetUpperBound(0);
		if (num2 <= 0 && num <= 0)
		{
			return null;
		}
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		if (num2 <= 0)
		{
			string[] array3 = array[num - 1].Split(':');
			text6 = array3[0];
			text4 = array3[1];
			text3 = array3[2];
			text2 = array3[3];
			text = array3[4];
			analyzedThreadClass.Category = "RPCIN";
			analyzedThreadClass.KeyPartTwo = "";
			stringBuilder.Append("processing an inbound RPC call");
			analyzedThreadClass.KeyPartOne = text6 + ":" + text3;
			if (text6 == "")
			{
				return null;
			}
			stringBuilder.Append(" over");
			switch (text6.ToUpper())
			{
			case "NCACN_IP_TCP":
				stringBuilder.Append(" <b>TCP/IP</b>");
				break;
			case "NCALRPC":
				stringBuilder.Append(" <b>LRPC</b>");
				break;
			case "NCACN_NP":
				stringBuilder.Append(" <b>named pipes</b>");
				break;
			case "NCACN_HTTP":
				stringBuilder.Append(" <b>HTTP</b>");
				break;
			default:
				stringBuilder.Append(" the <b>" + text6 + "</b> protocol");
				break;
			}
			if (text3.GetSafeLength() != 0)
			{
				stringBuilder.Append(" to the <B>" + text3 + "</B> endpoint.");
			}
			stringBuilder2.Append("The client application making this incoming RPC call may block while this server application completes the processing of the call.  Review the call stack for this thread to ensure that the it is processing the RPC call properly, and not causing unwanted delays in the client application.");
		}
		else
		{
			string[] array4 = array2[0].Split(':');
			text6 = array4[0];
			text4 = array4[1];
			text3 = array4[2];
			text5 = array4[3];
			analyzedThreadClass.Category = "RPCOUT";
			analyzedThreadClass.KeyPartOne = text6 + ":" + text4;
			analyzedThreadClass.KeyPartTwo = "";
			stringBuilder.Append("making an outbound RPC call");
			if (text6 == "")
			{
				return null;
			}
			stringBuilder.Append(" over");
			switch (text6.ToUpper())
			{
			case "NCACN_IP_TCP":
				stringBuilder.Append(" <b>TCP/IP</b>");
				break;
			case "NCALRPC":
				stringBuilder.Append(" <b>LRPC</b>");
				break;
			case "NCACN_NP":
				stringBuilder.Append(" <b>named pipes</b>");
				break;
			case "NCACN_HTTP":
				stringBuilder.Append(" <b>HTTP</b>");
				break;
			default:
				stringBuilder.Append(" the <b>" + text6 + "</b> protocol");
				break;
			}
			stringBuilder2.Append(" <b>Note</b> - additional RPC call information is also available in the thread details section of each thread listed in the Description pane to the left.");
		}
		if (num2 > 0)
		{
			text9 = "";
			if (num2 > 1)
			{
				text9 = "s";
			}
			stringBuilder3.Append("<BR><B>Outbound RPC Call" + text9 + ":</B><BR><table border=0 class=myCustomText>");
			for (num3 = 1; num3 <= num2; num3++)
			{
				string[] array5 = array2[num3 - 1].Split(':');
				text6 = array5[0];
				text4 = array5[1];
				text3 = array5[2];
				text5 = array5[3];
				text2 = array5[4];
				text = array5[5];
				text10 = ((!(text6.ToUpper() == "NCACN_IP_TCP")) ? "Endpoint" : "TCP Port");
				stringBuilder3.Append("<TR><TD>Protocol Sequence&nbsp;&nbsp;&nbsp;</TD><TD><B>" + text6 + "</B></TD></TR>");
				if (text4.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Network Address</TD><TD>" + text4 + "</B></TD></TR>");
				}
				if (text3.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>" + text10 + "</TD><TD>" + text3 + "</B></TD></TR>");
				}
				if (text5.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Options</TD><TD>" + text5 + "</TD></TR>");
				}
				if (text2.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Destination Process ID</TD><TD>" + text2 + "</TD></TR>");
				}
				if (text.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Destination Thread ID</TD><TD>" + text + "</TD></TR>");
				}
				stringBuilder3.Append("<TR><TD>&nbsp;</TD></TR>");
			}
			stringBuilder3.Append("</TABLE><br>");
		}
		if (num > 0)
		{
			text9 = "";
			if (num > 1)
			{
				text9 = "s";
			}
			stringBuilder3.Append("<BR><B>Inbound RPC Call" + text9 + ":</B><BR><table cellpadding=0 cellspacing=0 border=0 class=myCustomText>");
			for (num3 = 1; num3 <= num; num3++)
			{
				string[] array6 = array[num3 - 1].Split(':');
				text6 = array6[0];
				text4 = array6[1];
				text3 = array6[2];
				text2 = array6[3];
				text = array6[4];
				text10 = ((!(text6.ToUpper() == "NCACN_IP_TCP")) ? "Endpoint" : "TCP Port");
				stringBuilder3.Append("<TR><TD>Protocol Sequence&nbsp;&nbsp;&nbsp;</TD><TD><B>" + text6 + "</B></TD></TR>");
				if (text4.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Network Address</TD><TD>" + text4 + "</B></TD></TR>");
				}
				if (text3.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>" + text10 + "</TD><TD>" + text3 + "</B></TD></TR>");
				}
				if (text2.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Source Process ID</TD><TD>" + text2 + "</TD></TR>");
				}
				if (text.GetSafeLength() != 0)
				{
					stringBuilder3.Append("<TR><TD>Source Thread ID</TD><TD>" + text + "</TD></TR>");
				}
				stringBuilder3.Append("<TR><TD>&nbsp;</TD></TR>");
			}
			stringBuilder3.Append("</TABLE><br>");
		}
		analyzedThreadClass.Description = stringBuilder.ToString();
		analyzedThreadClass.Recommendation = stringBuilder2.ToString();
		analyzedThreadClass.AdditionalInfo = stringBuilder3.ToString();
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getXmlHTTPAnalysis(CacheFunctions.ScriptThreadClass Thread, int nFrameNum)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		bool flag = false;
		string[] array = new string[5] { "w3wp", "dllhost", "inetinfo", "aspnet_wp", "iisexpress" };
		foreach (string value in array)
		{
			if (Globals.g_Debugger.ExecutableName.IndexOf(value) >= 0)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return new AnalyzedThreadClass
			{
				Thread = Thread,
				Category = "XMLHTTP",
				IsWarning = true,
				Description = "making a call to a remote server using <b>XMLHTTP</b>",
				Recommendation = "XMLHTTP uses <b>WinInet.dll</b> to make connections to a remote server and WinInet functions are not supported when run from a service or an Internet Information Server (IIS) application (also a service). Instead of using XMLHTTP from a server application, you should switch to using ServerXMLHttp which is designed specifically for server side applications.<br><br>For more details refer to the below articles <br><ul><li>Frequently asked questions about ServerXMLHTTP <a href='http://support.microsoft.com/kb/290761'>http://support.microsoft.com/kb/290761</a></li><li>INFO: WinInet Not Supported for Use in Services <a href='http://support.microsoft.com/kb/238425'>http://support.microsoft.com/kb/238425</a></li></ul>"
			};
		}
		return null;
	}

	public AnalyzedThreadClass getIsapiAnalysis(CacheFunctions.ScriptThreadClass Thread, CacheFunctions.ScriptModuleClass Module)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		analyzedThreadClass.Category = "ISAPI";
		analyzedThreadClass.ClientConnInfo = Globals.g_HTTPInfo.GetClientConnectionByThreadID(Thread.ThreadID);
		analyzedThreadClass.ASPRequestInfo = Globals.g_ASPInfo.GetASPRequestByThreadID(Thread.ThreadID);
		if (Module.IsISAPIFilter)
		{
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "calling an ISAPI Filter";
			analyzedThreadClass.Recommendation = "ISAPI filters by design \"filter\" each and every request to the web service as it comes in and each and every response as it is sent back to web clients. Although this design allows developers to add useful functionality to IIS it also means that a faulty ISAPI filter can cause <b>all</b> the requests to the web service to stop responding. ";
			if (analyzedThreadClass.ClientConnInfo == null)
			{
				analyzedThreadClass.Category = "OK";
			}
		}
		else if (Module.IsISAPIExtension)
		{
			analyzedThreadClass.Description = "calling an ISAPI Extension";
			analyzedThreadClass.Recommendation = "The purpose of ISAPI extensions is to \"extend\" the functionality of a web site. Without ISAPI extensions the IIS web service would be similar to the FTP service where it simply returns static (unchanging) files. If a developer of an ISAPI extension has not written any sort of custom \"thread pool\" then the ISAPI extension they wrote will execute on the default threads that handle the incoming requests (IIS ThreadPool Thread) of the web service. The maximum number of threads in the IIS thread pool is 256 but is typically much lower due to system resources. If enough IIS ThreadPool threads become unavailable due to hanging requests to an ISAPI extension then soon IIS will have no threads available to handle any requests.";
			if (analyzedThreadClass.ClientConnInfo == null && analyzedThreadClass.ASPRequestInfo == null && Thread.StackFrames.Count < 7)
			{
				analyzedThreadClass.Category = "OK";
			}
		}
		analyzedThreadClass.Description = analyzedThreadClass.Description + " " + Module.ModuleName;
		analyzedThreadClass.Recommendation = analyzedThreadClass.Recommendation + " " + Globals.HelperFunctions.GetVendorMessage(Module.Base);
		analyzedThreadClass.KeyPartOne = Module.ModuleName;
		analyzedThreadClass.KeyPartTwo = "";
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getWinHttpAnalysis(CacheFunctions.ScriptThreadClass Thread, int FrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		double directCaller = Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "WINHTTP", FrameID);
		if (CacheFunctions.GetSymbolFromAddress(directCaller).ToUpper().IndexOf("KERNEL32!BASETHREADSTART") >= 0)
		{
			analyzedThreadClass.Category = "WINHTTP";
			analyzedThreadClass.IsWarning = true;
			analyzedThreadClass.Description = "making a network using WinHTTP library. The call to WinHTTP originated from <b>" + CacheFunctions.GetSymbolFromAddress(directCaller) + "</b>";
			analyzedThreadClass.Recommendation = "Using WinHTTP in server-side applications is the recommended alternative to using Wininet. If you are experiencing slow response time from IIS, the problem could be due to WinHTTP calling a remote server which is slow to respond, therefore, you may want to verify network latency of the applicable remote machine is at a minimum." + Globals.HelperFunctions.GetVendorMessage(directCaller);
			analyzedThreadClass.KeyPartOne = directCaller.ToString();
		}
		else
		{
			analyzedThreadClass.Category = "OK";
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getWinInetAnalysis(CacheFunctions.ScriptThreadClass Thread, int nFrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		analyzedThreadClass.Category = "WININET";
		analyzedThreadClass.IsWarning = true;
		double directCaller = Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "WININET", nFrameID);
		scriptModuleClass = CacheFunctions.GetModuleFromAddress(directCaller);
		if (scriptModuleClass != null)
		{
			if (scriptModuleClass.VSCompanyName.ToUpper().IndexOf("MICROSOFT") >= 0)
			{
				analyzedThreadClass.IsWarning = false;
			}
		}
		else if (Globals.g_Debugger.ExecutableName.ToUpper().IndexOf("IEXPLORE") >= 0)
		{
			analyzedThreadClass.IsWarning = false;
		}
		analyzedThreadClass.Description = "making a network using WinInet library. The call to Wininet originated from <b>" + CacheFunctions.GetSymbolFromAddress(directCaller) + "</b>";
		analyzedThreadClass.Recommendation = "Using WinInet in server side applications to make network calls is not recommended since Wininet was designed for client side applications in mind. For example, Wininet limits the number of simultaneous connections to a web server to not more than 2. This can have a serious performance impact causing hang like symptoms." + Globals.HelperFunctions.GetVendorMessage(directCaller);
		analyzedThreadClass.KeyPartOne = directCaller.ToString();
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getSocketAnalysis(CacheFunctions.ScriptThreadClass Thread, int nFrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		string text = null;
		string text2 = null;
		string text3 = null;
		string[] array = null;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		text2 = "RPCLTSCM!COMMON_SERVERRECEIVEANY;INETSLOC!SOCKETLISTENTHREAD;ISATQ!ATQ_BMON_SET::BMONTHREADFUNC";
		_ = Thread.StackFrames;
		analyzedThreadClass.Category = "SOCKET";
		analyzedThreadClass.IsWarning = true;
		for (int i = nFrameID; i < Thread.StackFrames.Count; i++)
		{
			text = CacheFunctions.GetFunctionName(Thread.StackFrames[i].InstructionAddress);
			switch (text)
			{
			case "WSOCK32!RECV":
			case "WS2_32!RECV":
			case "WS2_32!SELECT":
			case "WS2_32!WSARECV":
				break;
			default:
				continue;
			}
			double addr = ((!(text == "WS2_32!WSARECV")) ? Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "WS2_32", i) : Globals.HelperFunctions.GetDirectCaller(Thread.StackFrames, "WSOCK", i));
			string symbolFromAddress = CacheFunctions.GetSymbolFromAddress(addr);
			if (!Globals.HelperFunctions.IsNullOrEmpty(symbolFromAddress) && symbolFromAddress != "0x00000000")
			{
				analyzedThreadClass.Description = "waiting on data to be returned from another server via WinSock.<br><br> The call to WinSock originated from <b>" + CacheFunctions.GetSymbolFromAddress(addr) + "</b>";
			}
			else
			{
				analyzedThreadClass.Description = "waiting on data to be returned from another server via WinSock.<br><br> The caller to WinSock could not be determined";
			}
			analyzedThreadClass.Recommendation = "Ensure that any remote server this application may be calling is functioning properly and there are no network issues between the two servers. If the problem continues, please contact the application vendor for further assistance";
			analyzedThreadClass.KeyPartOne = addr.ToString();
			if (text2.IndexOf(CacheFunctions.GetFunctionName(addr).ToUpper()) >= 0 || Thread.StackFrames.Count - i < 4)
			{
				analyzedThreadClass.Category = "OK";
			}
			break;
		}
		if (analyzedThreadClass.Recommendation.GetSafeLength() == 0)
		{
			analyzedThreadClass.IsWarning = false;
			analyzedThreadClass.Description = "waiting on a socket function to complete. Further review of these threads may be necessary to determine what they are waiting on.";
			analyzedThreadClass.Recommendation = "";
			analyzedThreadClass.Category = "OK";
		}
		else
		{
			string socketSourceAddress = Thread.SocketSourceAddress;
			if (Convert.ToString(socketSourceAddress).GetSafeLength() >= 6 && socketSourceAddress.ToString().Substring(0, 6).ToUpper() != "ERROR:")
			{
				text3 = "<b>Socket</b> properties</b>:<br><table border=0 cellpadding=0 cellspacing=0 class=myCustomText>";
				array = socketSourceAddress.ToString().Split(':');
				if (array.GetUpperBound(0) == 1)
				{
					if (array[0] != "0.0.0.0")
					{
						text3 = text3 + "<tr><td>Source IP</td><td>" + array[0] + "</td></tr>";
					}
					text3 = text3 + "<tr><td>Source Port</td><td>" + array[1] + "</td></tr>";
				}
				else
				{
					text3 = text3 + "<tr><td>Source Address</td><td>" + socketSourceAddress.ToString() + "</td></tr>";
				}
			}
			socketSourceAddress = Thread.SocketDestinationAddress;
			if (Convert.ToString(socketSourceAddress).GetSafeLength() >= 6 && socketSourceAddress.ToString().Substring(0, 6).ToUpper() != "ERROR:")
			{
				analyzedThreadClass.KeyPartTwo = socketSourceAddress.ToString();
				analyzedThreadClass.Description += " and is destined for ";
				array = socketSourceAddress.ToString().Split(':');
				if (array.GetUpperBound(0) == 1)
				{
					text3 = text3 + "<tr><td>Destination IP</td><td>" + array[0] + "</td></tr>";
					text3 = text3 + "<tr><td>Destination Port&nbsp;&nbsp;&nbsp;</td><td>" + array[1] + "</td></tr></table>";
					analyzedThreadClass.Description = analyzedThreadClass.Description + "<b>port " + array[1] + "</b> at IP address <b>" + array[0] + "</b><br>";
				}
				else
				{
					text3 = text3 + "<tr><td>Source Address</td><td>" + socketSourceAddress.ToString() + "</td></tr></table>";
					analyzedThreadClass.Description = analyzedThreadClass.Description + "<b>" + socketSourceAddress.ToString() + "</b>";
				}
			}
			analyzedThreadClass.AdditionalInfo = text3 + "<br>";
		}
		return analyzedThreadClass;
	}

	public AnalyzedThreadClass getAdoDbAnalysis(CacheFunctions.ScriptThreadClass Thread, int nFrameID)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		int num = 0;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary = null;
		object obj = null;
		string text = null;
		double num2 = 0.0;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		int num3 = 0;
		analyzedThreadClass = new AnalyzedThreadClass();
		analyzedThreadClass.Thread = Thread;
		dictionary = Thread.StackFrames;
		_ = Thread.ThreadID;
		analyzedThreadClass.Category = "ADO";
		num = GetDirectCallerFrame(dictionary, "MSADO15", nFrameID);
		double instructionAddress = dictionary[num].InstructionAddress;
		text = CacheFunctions.GetFunctionName(dictionary[num - 1].InstructionAddress);
		if (text.ToString().IndexOf("::INVOKE") >= 0)
		{
			text = CacheFunctions.GetFunctionName(dictionary[num - 2].InstructionAddress);
		}
		analyzedThreadClass.Description = "making a database operation using ADO.<br><br> The call to <b>" + text.ToString() + "</b> originated from <b>" + CacheFunctions.GetSymbolFromAddress(instructionAddress) + "</b>";
		analyzedThreadClass.KeyPartOne = instructionAddress.ToString();
		analyzedThreadClass.Recommendation = "Ensure that the database this application is connecting to is responding as expected. Any network latency between the client machine and the database server may result in slow performance of the client application in question.";
		double childEBP = dictionary[nFrameID].ChildEBP;
		double childEBP2 = dictionary[num].ChildEBP;
		text5 = Globals.g_Debugger.Execute("ddp 0n" + childEBP + " 0n" + childEBP2);
		num3 = text5.LastIndexOf("msado15!ATL::CComObject<CRecordset>::`vftable'", StringComparison.OrdinalIgnoreCase) + 1;
		if (num3 > 0)
		{
			double num4 = Globals.HelperFunctions.FromHex(text5.Substring(num3 - 18 - 1, 8));
			num4 = GetAdoPointer(num4, "AdoRecordset");
			num2 = num4;
			text4 = GetAdoInfo(num2, "AdoRecordset");
		}
		num3 = text5.LastIndexOf("msado15!ATL::CComObject<CCommand>::`vftable'", StringComparison.OrdinalIgnoreCase) + 1;
		if (num3 > 0)
		{
			double num4 = Globals.HelperFunctions.FromHex(text5.Substring(num3 - 18 - 1, 8));
			num4 = GetAdoPointer(num4, "AdoCommand");
			num2 = num4;
			text3 = GetAdoInfo(num2, "AdoCommand");
		}
		num3 = text5.LastIndexOf("msado15!ATL::CComObject<CConnection>::`vftable'", StringComparison.OrdinalIgnoreCase) + 1;
		if (num3 > 0)
		{
			double num4 = Globals.HelperFunctions.FromHex(text5.Substring(num3 - 18 - 1, 8));
			num4 = GetAdoPointer(num4, "AdoConnection");
			num2 = num4;
			text2 = GetAdoInfo(num2, "AdoConnection");
		}
		obj = analyzedThreadClass.AdditionalInfo;
		analyzedThreadClass.AdditionalInfo = string.Concat(obj, text4, text3, text2);
		if (analyzedThreadClass.AdditionalInfo != "")
		{
			analyzedThreadClass.AdditionalInfo += "<br>";
		}
		return analyzedThreadClass;
	}

	public double GetAdoPointer(object Address, string objectType)
	{
		string text = null;
		int num = 0;
		switch (objectType)
		{
		case "AdoRecordset":
			text = Globals.g_Debugger.Execute("dds " + Address.ToString() + "-0x74 " + Address.ToString() + "+0x10");
			num = text.ToString().IndexOf("msado15!ATL::CComObject<CRecordset>::`vftable'") + 1;
			break;
		case "AdoCommand":
			text = Globals.g_Debugger.Execute("dds " + Address.ToString() + "-0x38 " + Address.ToString() + "+0x10");
			num = text.ToString().IndexOf("msado15!ATL::CComObject<CCommand>::`vftable'") + 1;
			break;
		case "AdoConnection":
			text = Globals.g_Debugger.Execute("dds " + Address.ToString() + "-0x5c " + Address.ToString() + "+0x10");
			num = text.IndexOf("msado15!ATL::CComObject<CConnection>::`vftable'") + 1;
			break;
		}
		if (num > 0)
		{
			return Convert.ToDouble(text.Substring(num - 19 - 1, 8));
		}
		return 0.0;
	}

	public int GetDirectCallerFrame(Dictionary<int, CacheFunctions.ScriptStackFrameClass> StackFrames, string NotLikeFunction, object StackFrameNumber)
	{
		int num = 0;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		int num2 = 0;
		num = 0;
		for (num2 = Convert.ToInt32(StackFrameNumber); num2 < StackFrames.Count; num2++)
		{
			scriptStackFrameClass = StackFrames[num2];
			if (Globals.g_Debugger.GetSymbolFromAddress(scriptStackFrameClass.ReturnAddress).ToUpper().IndexOf(NotLikeFunction) == -1)
			{
				return num2 + 1;
			}
		}
		return num;
	}

	public string GetAdoInfo(double AdoObject, string objectType)
	{
		AdoOffsets adoOffsets = null;
		if (adoOffsets == null)
		{
			return null;
		}
		if (adoOffsets != null && adoOffsets == null)
		{
			return null;
		}
		string result = "";
		switch (objectType)
		{
		case "AdoConnection":
		{
			double num = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_connInfo);
			string text2 = Globals.g_Debugger.ReadUnicodeString(num);
			if (text2 == "")
			{
				text2 = "&nbsp;";
			}
			double num3 = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_lLoginTimeout);
			double num4 = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_lCommandTimeout);
			result = "<BR><B>ADO Connection</B> properties: <BR><table cellpadding=0 cellspacing=0 border=0 class=myCustomText><TR><TD>Connection String</TD><TD><B>" + Globals.HelperFunctions.MaskPwd(text2) + "</B></TD></TR><TR><TD>Connection Timeout&nbsp;&nbsp;&nbsp;</TD><TD>" + num3 + "s</TD></TR><TR><TD>Command Timeout </TD><TD>" + num4 + "s</TD></TR></TABLE>";
			break;
		}
		case "AdoCommand":
		{
			double num = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_strSQL);
			string text = Globals.g_Debugger.ReadUnicodeString(num);
			if (text == "")
			{
				text = "&nbsp;";
			}
			double num4 = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_lQueryTimeout);
			result = "<BR><B>ADO Command</B> properties: <BR><table cellpadding=0 cellspacing=0 border=0 class=myCustomText><TR><TD>Query</TD><TD><B>" + text + "</B></TD></TR><TR><TD>Timeout </TD><TD>" + num4 + "s</TD></TR></TABLE>";
			break;
		}
		case "AdoRecordset":
		{
			double num = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_sstrSource);
			string text = Globals.g_Debugger.ReadUnicodeString(num);
			if (text == "")
			{
				text = "&nbsp;";
			}
			double num2 = Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_fOpen);
			num2 = (int)num2;
			ADOCursorLocation aDOCursorLocation = (ADOCursorLocation)Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_eCursorLocation);
			ADOCursorType aDOCursorType = (ADOCursorType)Globals.g_Debugger.ReadDWord(AdoObject + adoOffsets.m_lCursorType);
			result = "<BR><B>ADO Recordset</B> properties: <BR><table cellpadding=0 cellspacing=0 border=0 class=myCustomText><TR><TD>Query</TD><TD><B>" + text + "</B></TD></TR><TR><TD>CursorLocation&nbsp;&nbsp;&nbsp;</TD><TD>" + aDOCursorLocation.ToString() + "</TD></TR><TR><TD>CursorType</TD><TD>" + aDOCursorType.ToString() + "</TD></TR><TR><TD>State</TD><TD>" + num2 + "</TD></TR></TABLE>";
			break;
		}
		}
		return result;
	}

	public static string GetTEBAsHex(CacheFunctions.ScriptThreadClass Thread)
	{
		string text = Globals.HelperFunctions.DebuggerExecuteReplaceLF("~" + Thread.ThreadID + "e r $teb", "");
		if (text.Contains("$teb"))
		{
			return text.Substring(5, Globals.g_SizeOfULongPtr * 2);
		}
		return "";
	}

	public bool IsThreadSTA(CacheFunctions.ScriptThreadClass Thread)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		bool result = false;
		if (Globals.g_collCOMPlusSTAThreadPoolThreads.ContainsKey(Thread.ThreadID))
		{
			result = true;
		}
		else
		{
			double num4;
			double num5;
			double num6;
			if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
			{
				num4 = 5976.0;
				num5 = 16.0;
				num6 = 128.0;
			}
			else
			{
				num4 = 3968.0;
				num5 = 12.0;
				num6 = ((Globals.g_OSVER == Globals.OS_VER_WIN2K) ? 68 : 80);
			}
			num = Globals.HelperFunctions.FromHex(GetTEBAsHex(Thread));
			if (num > 0.0)
			{
				if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
				{
					num2 = Globals.g_Debugger.ReadQWord(num + num4);
					if (num2 > 0.0)
					{
						num3 = Globals.g_Debugger.ReadQWord(num2 + num6);
						if (num3 > 0.0 && Globals.g_Debugger.ReadDWord(num3 + num5) == 4.0)
						{
							return true;
						}
					}
				}
				else
				{
					num2 = Globals.g_Debugger.ReadDWord(num + num4);
					if (num2 > 0.0)
					{
						num3 = Globals.g_Debugger.ReadDWord(num2 + num6);
						if (num3 > 0.0 && Globals.g_Debugger.ReadDWord(num3 + num5) == 4.0)
						{
							return true;
						}
					}
				}
			}
		}
		return result;
	}

	public string GetTEB(CacheFunctions.ScriptThreadClass Thread)
	{
		string tEBAsHex = GetTEBAsHex(Thread);
		if (!tEBAsHex.Equals(""))
		{
			return Globals.HelperFunctions.FromHex(tEBAsHex).ToString();
		}
		return "";
	}
}
