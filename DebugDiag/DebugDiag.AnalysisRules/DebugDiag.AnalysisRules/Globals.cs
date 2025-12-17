using System.Collections.Generic;
using System.Text;
using ComplusDDExt;
using CrashHangExtLib;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using IISInfoLib;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

internal class Globals
{
	public const int THREADNUM_INVALID = -1;

	public const int FINDFRAME_NONEFOUND = -1;

	public const bool TRACE_ON = false;

	public static string NOT_FOUND = "NOT_FOUND";

	public static int ANALYSIS_STEP_COUNT = 28;

	public static int g_RequestTimeLimit = 90;

	public static bool g_DoCombinedNativeMangedPerfAnalysis = false;

	public static bool g_IgnoreCheckboxAndNeverIncludeSourceLineInfo = true;

	public static int MIN_PERF_DUMPS_FOR_ANALYSIS = 1;

	public static bool VERIFY_TARGET_PROCESS = false;

	public static int TopModuleLimit = 10;

	public static int TopHeapLimit = 10;

	public static bool UseKnownGoodModList = true;

	public static int TopSizeLimit = 10;

	public static int TopLeakProbability = 75;

	public static int TopLeakyFunctionLimit = 2;

	public static int MaxNumberCallStacks = 2;

	public static int TopNumFunctionsLimit = 5;

	public static int PERF_ANALYSIS_STEP_COUNT_OVERALL = 5;

	public static int PERF_ANALYSIS_STEP_COUNT_REPEAT_EACH_DUMP = 2;

	public static NetScriptManager Manager;

	public static NetProgress g_Progress;

	public static NetDbgObj g_Debugger;

	public static Utils g_UtilExt;

	public static ILeakTrackInfo g_LeakTrackInfo;

	public static IHeapInfo g_HeapInfo;

	public static IHTTPInfo g_HTTPInfo;

	public static IASPInfo g_ASPInfo;

	public static VMInfo g_VMInfo;

	public static IComplusRoot g_ComplusExt;

	public static bool g_HideDotNetReportInfo = false;

	public static int g_OverallProgress = 0;

	public static string g_DataFile = string.Empty;

	public static int g_lTraceKey = 0;

	public static bool g_ExtendedThreadInfoAvailable = false;

	public static string g_oldVBRuntime = string.Empty;

	public static string g_ShortDumpFileName = string.Empty;

	public static bool g_IsDebugEnabled = false;

	public static int g_GlobalFlagsValue = 0;

	public static int g_LongRunningClientConns = 0;

	public static int g_LongRunningASPReq = 0;

	public static bool g_IsBlockingIssueDetected = false;

	public static string g_UniqueReference = string.Empty;

	public static string g_OSPlatformVersion = string.Empty;

	public static int g_SizeOfULongPtr = 0;

	public static IDbgModule g_clrModule = null;

	public static string HighFragHeaps = string.Empty;

	public static int[] g_MaxConnectionErrorThreads = null;

	public static int[] g_MaxConnectionWarningThreads = null;

	public static bool g_CLRExtensionExecuting = false;

	public static int g_GCThread = -1;

	public static string g_PreemptiveGCDisabledThreads = string.Empty;

	public static int g_FinalizerThreadId = -1;

	public static bool g_FinalizerThreadBlocked = false;

	public static bool g_AttemptedToLoadSOS = false;

	public static bool g_IsWCFServiceHost = false;

	public static bool g_IsWCFClient = false;

	public static bool g_WcfRequestSummaryPresent = false;

	public static bool g_2and4frameworkSxsFailure = false;

	public static bool g_ManagedExceptionsPresent = false;

	public static bool g_HttpRequestsQueued = false;

	public static int g_threadCountManagedRunningMoreThan60Secs = 0;

	public static bool g_IsSystemWebApp = false;

	public static bool g_IsIIS7 = false;

	public static bool g_HttpContextsPresent = false;

	public static long g_HttpContextMT = -1L;

	public static bool g_ADODotNetConnectionsPresent = false;

	public static int intPosition = -1;

	public static int intLen = -1;

	public static int filecount = -1;

	public static string g_SubStatusCaption = string.Empty;

	public static string g_subStatusTitle = string.Empty;

	public static int g_SubStatusMaxPosition = -1;

	public static int g_SubStatusPosition = -1;

	public static bool g_IsWow64Dump = false;

	public static string g_RelatedIssuesReport = string.Empty;

	public static bool g_RelatedIssuesFound = false;

	public static string g_COMRuntimeModule = string.Empty;

	public static Dictionary<double, CacheFunctions.ScriptThreadClass> g_collExceptionThreads = new Dictionary<double, CacheFunctions.ScriptThreadClass>();

	public static Dictionary<string, string> g_collPreviousExceptions = new Dictionary<string, string>();

	public static Dictionary<double, CacheFunctions.ScriptThreadClass> g_collThreadsBlockedByCritsecs = new Dictionary<double, CacheFunctions.ScriptThreadClass>();

	public static Dictionary<double, IDbgCritSec> g_collCritSecs = new Dictionary<double, IDbgCritSec>();

	public static Dictionary<double, double> g_collThreadsBlockedByThisCritsec = new Dictionary<double, double>();

	public static Dictionary<double, CacheFunctions.ScriptThreadClass> g_collDeadLockedThreads = new Dictionary<double, CacheFunctions.ScriptThreadClass>();

	public static Dictionary<double, IDbgCritSec> g_collKnownCSIssueFound = new Dictionary<double, IDbgCritSec>();

	public static BarGraph g_HeapGraph = new BarGraph();

	public static QueuedReportsClass g_QueuedReports = new QueuedReportsClass();

	public static Dictionary<int, string> g_DumpStackObjects = new Dictionary<int, string>();

	public static Dictionary<double, int> g_HttpContextThreads;

	public static Dictionary<double, int> g_WCFIOSchedulerThreads = new Dictionary<double, int>();

	public static Dictionary<double, Dictionary<string, string>> g_CLRObjectOffSets = new Dictionary<double, Dictionary<string, string>>();

	public static Dictionary<object, string> g_ThreadOwningSyncBlk = new Dictionary<object, string>();

	public static HashSet<string> g_ThreadsWithSyncBlkWaiters = new HashSet<string>();

	public static Dictionary<int, object> g_ThreadWaitingOnSyncBlk = new Dictionary<int, object>();

	public static Dictionary<int, string> g_ThreadExceptionList = new Dictionary<int, string>();

	public static Dictionary<string, string> g_ManagedThreads;

	public static Dictionary<string, string> g_ThreadPoolIOThreads;

	public static Dictionary<string, string> g_ThreadPoolWorkerThreads = new Dictionary<string, string>();

	public static StringBuilder g_CLRAnalysisReport = new StringBuilder();

	public static StringBuilder g_HttpRequestQueueTable = new StringBuilder();

	public static Dictionary<string, string> g_ManagedDumpMTCache;

	public static Dictionary<string, string> g_ManagedIp2mdCache;

	public static Dictionary<string, string> g_WellKnownCLRExceptionsReported = new Dictionary<string, string>();

	public static Dictionary<double, string> g_SysNetConnectionWithBuffer = new Dictionary<double, string>();

	public static Dictionary<string, int> g_SysNetServicePointsWaitingOnSocket = new Dictionary<string, int>();

	public static INTHeap[] g_HeapArray;

	public static INTHeap2[] g_NTHeapArray;

	public static Dictionary<double, ILTModule> g_ModuleArray;

	public static Dictionary<double, ILTType> g_LTTypeArray;

	public static int g_Weight;

	public static int g_StepCount;

	public static bool g_bFunctionTracked;

	public static bool g_bStackCollected;

	public const int LTT_HEAP = 0;

	public const int LTT_CRT = 1;

	public const int LTT_OLE = 2;

	public const int LTT_BSTR = 3;

	public const int LTT_VMM = 4;

	public const int LTHT_SOCKET = 5;

	public const int LTHT_MUTEX = 6;

	public const int LTHT_SEMAPHORE = 7;

	public const int LTHT_EVENT = 8;

	public const int LTHT_TIMER_QUEUE = 9;

	public const int LTHT_TIMER_QUEUE_TIMER = 10;

	public const int LTHT_WAITABLE_TIMER = 11;

	public const int LTHT_FILE = 12;

	public const int LTHT_FILE_MAPPING = 13;

	public const int LTHT_IO_COMPLETION_PORT = 14;

	public const int LTHT_ANONYMOUS_PIPE = 15;

	public const int LTHT_NAMED_PIPE = 16;

	public const int LTHT_SECURITY_TOKEN = 17;

	public const int LTHT_FILE_CHANGE_NOTIFY = 18;

	public const int LTHT_CONSOLE_SCREEN_BUFFER = 19;

	public const int LTHT_DESKTOP = 20;

	public const int LTHT_WINDOW_STATION = 21;

	public const int LTHT_EVENT_SOURCE = 22;

	public const int LTHT_THREAD = 26;

	public const int LTHT_PROCESS = 27;

	public const int LTHT_REGISTRY_KEY = 28;

	public const int ST_BYSIZE = 0;

	public const int ST_BYCOUNT = 1;

	public const int LTHT_EVENT_LOG = 23;

	public const int LTHT_JOB_OBJECT = 24;

	public const int LTHT_MAILSLOT = 25;

	public const int ST_BYPROBABILITY = 2;

	public static bool g_haveObjectsReadyForFinalization = false;

	public static bool g_webCache = false;

	public static AnalyzedThreadsClass g_AnalyzedThreads = new AnalyzedThreadsClass();

	public static COperations g_AllOperations = new COperations();

	public static CDumps g_dumps = new CDumps();

	public static bool g_IgnoreCheckboxAndNeverIncludeSourceLineInfoReported;

	public const int TopOperationsLimit = 10000;

	public const int TopUserFunctionsLimit = 100;

	public const int TopAllFunctionsLimit = 100;

	public const int TopFunctionsLimitMultiplier = 2;

	public const int STARTFRAME_USEALL = -1;

	public const string VBSCRIPT_RUN_FRAME = "VBSCRIPT!CSCRIPTRUNTIME::RUN";

	public static int[] TopOperationsThresholds = new int[4] { 0, 5, 5, 0 };

	public const int OPSTATS_VALUE_NONE = 0;

	public const int OPSTATS_VALUE_AVG_CPU = 1;

	public const int OPSTATS_VALUE_MAX_CPU = 2;

	public const int OPSTATS_VALUE_HITCOUNT = 3;

	public const int OPSTATS_VALUE_DURATION = 4;

	public const string OPSTATS_TITLE_AVG_CPU = "Threads By Avg CPU";

	public const string OPSTATS_TITLE_MAX_CPU = "Threads By Max CPU";

	public const string OPSTATS_TITLE_DURATION = "Operations By Duration";

	public const string OPSTATS_TITLE_HITCOUNT = "Operations By Hit Count (Frequency)";

	public const int STACKFRAME_PRI_BOILERPLATE = 1;

	public const int STACKFRAME_PRI_SYSTEM = 2;

	public const int STACKFRAME_PRI_USER = 3;

	public const int FNSTATS_USERONLY = 0;

	public const int FNSTATS_ALL = 1;

	public const int OPTYPE_DISCARDED = -1;

	public const int OPTYPE_UNINITIALIZED = 0;

	public const int OPTYPE_UNKNOWN = 1;

	public const string OPDESC_UNKNOWN = "Unknown Operation";

	public const string OPKEY_UNKNOWN = "UNK";

	public const int OPTYPE_ASP = 2;

	public const string OPDESC_ASP = "ASP Request";

	public const string OPKEY_ASP = "ASP";

	public const int OPTYPE_ASPNET = 3;

	public const string OPDESC_ASPNET = "ASP.NET Request";

	public const string OPKEY_ASPNET = "ASPNET";

	public const int OPTYPE_COM = 4;

	public const string OPDESC_COM = "Inbound COM Call";

	public const string OPKEY_COM = "COM";

	public const int OPTYPE_SQL = 5;

	public const string OPDESC_SQL = "Outbound SQL";

	public const string OPKEY_SQL = "SQL";

	public const int OPTYPE_COMPILATION = 6;

	public const string OPDESC_COMPILATION = "Code Compilation";

	public const string OPKEY_COMPILATION = "COMP";

	public const int OPTYPE_JSON = 7;

	public const string OPDESC_JSON = "Json Serialization";

	public const string OPKEY_JSON = "JSON";

	public const int MAX_TOP_USER_FUNCTIONS = 10;

	public const int FUNCTIONS_BY_HITCOUNT_CONTENTS_HITCOUNT = 0;

	public const int FUNCTIONS_BY_HITCOUNT_CONTENTS_PRIORITY = 1;

	public const string INPUT_SUMMARY = "Input Summary";

	public const string PERF_SUMMARY = "Performance Summary";

	public const string PERF_DETAILS = "Performance Details";

	public const string OPERATION_MAP = "Operation Map";

	public const string STAT_ROLLUPS = "Statistical Rollups";

	public const string REPORT_LEGEND = "Report Legend";

	public static int g_OSVER;

	public static IHelperFunctions HelperFunctions = new HelperFunctionsImpl();

	public static IHeapFunctions HeapFunctions = new HeapFunctionsImpl();

	public static IAnalyzeManaged AnalyzeManaged = new AnalyzeManagedImpl();

	public static IAnalyzeComPlus AnalyzeComPlus = new AnalyzeComPlusImpl();

	public static IAnalyzeCritSecs AnalyzeCritSecs = new AnalyzeCritSecsImpl();

	public static AnalyzeThreads AnalyzeThreads = new AnalyzeThreads();

	public static IAnalyzeActivities AnalyzeActivities = new AnalyzeActivitiesImpl();

	public static IReportHTTPInfo ReportHTTPInfo = new ReportHTTPInfoImpl();

	public static IReportASPInfo ReportASPInfo = new ReportASPInfoImpl();

	public static string vbcrlf = "\r\n";

	public static Dictionary<CDbgActivity, double> g_collDeadLockedActivites = new Dictionary<CDbgActivity, double>();

	public static Dictionary<CDbgActivity, double> g_collEnteredButNonDeadlockedActivites = new Dictionary<CDbgActivity, double>();

	public static Dictionary<CDbgActivity, double> g_collTransitioningActivites = new Dictionary<CDbgActivity, double>();

	public static bool g_IsComPlusSTAPoolIssueDetected;

	public static CacheFunctions.ScriptThreadsClass g_ThreadInfoCache = new CacheFunctions.ScriptThreadsClass();

	public static Dictionary<double, string> g_SymbolFromAddrCache = new Dictionary<double, string>();

	public static Dictionary<double, string> g_SymbolFromAddrIEReplacedCache = new Dictionary<double, string>();

	public static Dictionary<double, string> g_SourceInfoFromAddrCache = new Dictionary<double, string>();

	public static Dictionary<double, string> g_FunctionNameFromAddrCache = new Dictionary<double, string>();

	public static Dictionary<double, string> g_ModuleNameFromAddrCache = new Dictionary<double, string>();

	public static Dictionary<string, int> g_ThreadSearchCache = new Dictionary<string, int>();

	public static Dictionary<string, int> g_ClrThreadSearchCache = new Dictionary<string, int>();

	public static Dictionary<string, string> g_FunctionPrototypeWithoutOffsetCache = new Dictionary<string, string>();

	public static CacheFunctions.ScriptModulesClass g_ModuleCache = new CacheFunctions.ScriptModulesClass();

	public static Dictionary<int, string> g_collProcessesInclucedInThisReport = new Dictionary<int, string>();

	public static Dictionary<int, string> g_collTRACE = new Dictionary<int, string>();

	public static bool g_MixedComcallCritsecDeadlockDetected;

	public static Dictionary<double, double> g_collSpecialSTAThreads = new Dictionary<double, double>();

	public static int g_instanceCount;

	public static int g_inCallCount;

	public static int g_currentStaThreadCount;

	public static int g_maxStaThreadCount;

	public static int g_minStaThreadCount;

	public static int g_activitiesPerThread;

	public static bool g_emulateMTS;

	public static int g_staThreadsInCall;

	public static int g_boundThreadCount;

	public static Dictionary<int, int> g_colSTAThreadActivityCounts = new Dictionary<int, int>();

	public static int g_activityPileUpCount;

	public static Dictionary<int, int> g_colSTAThreadInCallStates = new Dictionary<int, int>();

	public static Dictionary<double, double> g_collCOMPlusSTAThreadPoolThreads = new Dictionary<double, double>();

	public static bool g_COMPlusSTAStackingPresent;

	public static COMPlusReportClass g_COMPlusReport = new COMPlusReportClass();

	public static bool g_COMSTAStackingPresent;

	public static string g_HostOffsetHex = string.Empty;

	public static string g_DebugDiagInstallPath = string.Empty;

	public static bool g_bShowTopImageGraph = false;

	public static int g_MinThreadOverride = -1;

	public static int g_MaxThreadOverride = -1;

	public static string ERROR_BAD_SYMBOLS = "ERROR_BAD_SYMBOLS";

	public const string SHORT_GC_RECOMMENDATION = "When a GC is running the .NET objects are not in a valid state and the reported analysis may be inaccurate. Also, the thread that triggered the Garbage collection may or may not be a problematic thread. Too many garbage collections in a process are bad for the performance of the application. Too many GC's in a process may indicate a memory pressure or symptoms of fragmenation. Review the blog <a href='http://blogs.msdn.com/tess/archive/2006/06/22/643309.aspx'>ASP.NET Case Study: High CPU in GC - Large objects and high allocation rates</a> for more details";

	public const string SHORT_GC_RECOMMENDATION_PREMPTIVE_THREADS = "Review the callstacks for the threads that have preemptive GC Disabled to see what they are doing which is preventing the GC to run. Review the blog <a href='http://blogs.msdn.com/tess/archive/2007/03/12/net-hang-case-study-the-gc-loader-lock-deadlock-a-story-of-mixed-mode-dlls.aspx'>.NET Hang Case Study: The GC-Loader Lock Deadlock (a story of mixed mode dlls)</a> that gives some information on how to debug these issue more";

	public const string SHORT_THREAD_SPINNING_TO_ENTER_A_LOCK = "spinning waiting to enter a .net lock";

	public const string SHORT_THREAD_WAITING_TO_ENTER_A_LOCK = "waiting to enter a .NET Lock";

	public const string SHORT_THREAD_WAITING_ON_DB_SERVER = "waiting on data to be returned from the database server";

	public const string SHORT_THREAD_OPENING_DB_CONNECTION = "trying to open a data base connection";

	public const string SHORT_THREAD_WAITING_WAITONE = "waiting in a WaitOne";

	public const string SHORT_RECOMMENDATION_THREAD_WAITING_DOTNET = "Typically threads waiting in WaitMultiple are monitoring threads in a process and this may be ignored, however too many threads waiting in WaitOne\\ WaitMultiple may be a problem. Review the callstack of the threads waiting to see what they are waiting on";

	public const string SHORT_THREAD_WAITING_WAITMULTIPE = "waiting in a WaitMultiple";

	public static int OS_VER_UNKNOWN => 0;

	public static int OS_VER_WIN2K => 1;

	public static int OS_VER_WINXP => 2;

	public static int OS_VER_WIN2K3 => 3;

	public static int OS_VER_WIN2K3SP => 4;

	public static int OS_VER_WINVISTA => 5;

	public static int OS_VER_WIN7 => 6;

	public static int OS_VER_WIN8 => 7;

	public static int OS_VER_WINNEW => 8;

	public static bool g_SymErrDisplayed { get; set; }
}
