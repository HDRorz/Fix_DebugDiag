using System;
using System.Collections.Generic;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

public class COperations : IHasTopUserFunctions
{
	private Dictionary<string, COperation> m_OperationsByAvgCpu;

	private Dictionary<string, COperation> m_OperationsByMaxCpu;

	private Dictionary<string, COperation> m_OperationsByIncrCpu;

	private Dictionary<string, COperation> m_operationsByDuration;

	private Dictionary<int, string> m_TypeStrings;

	private Dictionary<int, Dictionary<string, string>> m_boilerPlateFunctionsByOpType;

	private HashSet<string> m_AllBoilerPlateFunctions;

	private Dictionary<string, int[]> m_FunctionsByHitCount;

	private int m_TopUserFunctionWatermark;

	private string m_Title;

	private int m_ValueType;

	private Dictionary<int, int> hitcountPriority;

	public Dictionary<string, int[]> FunctionsByHitCount
	{
		get
		{
			if (m_FunctionsByHitCount.Count == 0 && m_operationsByDuration.Count > 0)
			{
				RollupFunctionsByHitCount();
			}
			return m_FunctionsByHitCount;
		}
	}

	public HashSet<string> AllBoilerPlateFunctions
	{
		get
		{
			if (m_AllBoilerPlateFunctions == null)
			{
				_ = BoilerPlateFunctionsByOpType;
				m_AllBoilerPlateFunctions = new HashSet<string>();
				foreach (int key in m_boilerPlateFunctionsByOpType.Keys)
				{
					foreach (string key2 in m_boilerPlateFunctionsByOpType[key].Keys)
					{
						if (!m_AllBoilerPlateFunctions.Contains(key2))
						{
							m_AllBoilerPlateFunctions.Add(key2);
						}
					}
				}
			}
			return m_AllBoilerPlateFunctions;
		}
	}

	public Dictionary<int, Dictionary<string, string>> BoilerPlateFunctionsByOpType
	{
		get
		{
			Dictionary<string, string> dictionary = null;
			if (m_boilerPlateFunctionsByOpType == null)
			{
				m_boilerPlateFunctionsByOpType = new Dictionary<int, Dictionary<string, string>>();
				dictionary = new Dictionary<string, string>();
				dictionary.AddOrUpdate("NTDLL!KIFASTSYSTEMCALLRET", "");
				dictionary.AddOrUpdate("MSVCRT!_THREADSTARTEX", "");
				dictionary.AddOrUpdate("KERNEL32!BASETHREADSTART", "");
				dictionary.AddOrUpdate("Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Storage.ConfigurationChangeWatcher.Poller(System.Object)", "");
				dictionary.AddOrUpdate("System.Threading.ExecutionContext.runTryCode(System.Object)", "");
				dictionary.AddOrUpdate("System.Runtime.CompilerServices.RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(TryCode, CleanupCode, System.Object)", "");
				dictionary.AddOrUpdate("System.Threading.ThreadHelper.ThreadStart_Context(System.Object)", "");
				dictionary.AddOrUpdate("System.Threading.ThreadHelper.ThreadStart(System.Object)", "");
				dictionary.AddOrUpdate("System.Net.TimerThread.ThreadProc()", "");
				dictionary.AddOrUpdate("System.Threading.ThreadHelper.ThreadStart()", "");
				dictionary.AddOrUpdate("System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object)", "");
				dictionary.AddOrUpdate("System.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object)", "");
				dictionary.AddOrUpdate("MSVBVM60!EPIINVOKEMETHOD", "");
				dictionary.AddOrUpdate("MSVBVM60!BASIC_CLASS_INVOKE", "");
				dictionary.AddOrUpdate("OLEAUT32!IDISPATCH_INVOKE_STUB", "");
				dictionary.AddOrUpdate("OLEAUT32!IDISPATCH_REMOTEINVOKE_THUNK", "");
				dictionary.AddOrUpdate("RPCRT4!NDRSTUBCALL2", "");
				dictionary.AddOrUpdate("RPCRT4!CSTDSTUBBUFFER_INVOKE", "");
				dictionary.AddOrUpdate("OLEAUT32!CSTUBWRAPPER::INVOKE", "");
				dictionary.AddOrUpdate("OLE32!SYNCSTUBINVOKE", "");
				dictionary.AddOrUpdate("OLE32!STUBINVOKE", "");
				dictionary.AddOrUpdate("OLE32!CCTXCOMCHNL::CONTEXTINVOKE", "");
				dictionary.AddOrUpdate("OLE32!MTAINVOKE", "");
				dictionary.AddOrUpdate("OLE32!STAINVOKE", "");
				dictionary.AddOrUpdate("OLE32!APPINVOKE", "");
				dictionary.AddOrUpdate("OLE32!COMINVOKEWITHLOCKANDIPID", "");
				dictionary.AddOrUpdate("OLE32!COMINVOKE", "");
				dictionary.AddOrUpdate("OLE32!THREADDISPATCH", "");
				dictionary.AddOrUpdate("OLE32!THREADWNDPROC", "");
				dictionary.AddOrUpdate("COMBASE!SYNCSTUBINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!STUBINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!CCTXCOMCHNL::CONTEXTINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!MTAINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!STAINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!APPINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!COMINVOKEWITHLOCKANDIPID", "");
				dictionary.AddOrUpdate("COMBASE!COMINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!THREADDISPATCH", "");
				dictionary.AddOrUpdate("COMBASE!THREADWNDPROC", "");
				dictionary.AddOrUpdate("USER32!INTERNALCALLWINPROC", "");
				dictionary.AddOrUpdate("USER32!USERCALLWINPROCCHECKWOW", "");
				dictionary.AddOrUpdate("USER32!DISPATCHMESSAGEWORKER", "");
				dictionary.AddOrUpdate("USER32!DISPATCHMESSAGEW", "");
				dictionary.AddOrUpdate("COMSVCS!CSTAQUEUELESSMESSAGEWORK::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::PROCESSQUEUEWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::WORKERLOOP", "");
				dictionary.AddOrUpdate("NTDLL!ZWREPLYWAITRECEIVEPORTEX", "");
				dictionary.AddOrUpdate("RPCRT4!LRPC_ADDRESS::RECEIVELOTSACALLS", "");
				dictionary.AddOrUpdate("RPCRT4!RECVLOTSACALLSWRAPPER", "");
				dictionary.AddOrUpdate("RPCRT4!BASECACHEDTHREADROUTINE", "");
				dictionary.AddOrUpdate("RPCRT4!THREADSTARTROUTINE", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREADPOOL::KILLTHREADCONTROLLOOP", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREADPOOL::LOADBALANCETHREADCONTROLLOOP", "");
				dictionary.AddOrUpdate("COMSVCS!POSTDATA", "");
				dictionary.AddOrUpdate("COMSVCS!WORK_QUEUE::THREADLOOP", "");
				dictionary.AddOrUpdate("COMSVCS!WORK_QUEUE::WORKERLOOP", "");
				dictionary.AddOrUpdate("KERNEL32!GETQUEUEDCOMPLETIONSTATUS", "");
				dictionary.AddOrUpdate("NTDLL!NTDELAYEXECUTION", "");
				dictionary.AddOrUpdate("NTDLL!NTREMOVEIOCOMPLETION", "");
				dictionary.AddOrUpdate("NTDLL!RTLPWORKERTHREAD", "");
				dictionary.AddOrUpdate("RPCRT4!TIMER::WAIT", "");
				dictionary.AddOrUpdate("USER32!NTUSERGETMESSAGE", "");
				dictionary.AddOrUpdate("OLE32!CDLLHOST::STAWORKERLOOP", "");
				dictionary.AddOrUpdate("OLE32!CDLLHOST::WORKERTHREAD", "");
				dictionary.AddOrUpdate("OLE32!DLLHOSTTHREADENTRY", "");
				dictionary.AddOrUpdate("OLE32!CRPCTHREAD::WORKERLOOP", "");
				dictionary.AddOrUpdate("OLE32!CRPCTHREADCACHE::RPCWORKERTHREADENTRY", "");
				dictionary.AddOrUpdate("COMBASE!CDLLHOST::STAWORKERLOOP", "");
				dictionary.AddOrUpdate("COMBASE!CDLLHOST::WORKERTHREAD", "");
				dictionary.AddOrUpdate("COMBASE!DLLHOSTTHREADENTRY", "");
				dictionary.AddOrUpdate("COMBASE!CRPCTHREAD::WORKERLOOP", "");
				dictionary.AddOrUpdate("COMBASE!CRPCTHREADCACHE::RPCWORKERTHREADENTRY", "");
				m_boilerPlateFunctionsByOpType.Add(1, dictionary);
				dictionary = new Dictionary<string, string>();
				dictionary.AddOrUpdate("VBSCRIPT!CSCRIPTRUNTIME::RUN", "");
				dictionary.AddOrUpdate("VBSCRIPT!CSCRIPTENTRYPOINT::CALL", "");
				dictionary.AddOrUpdate("VBSCRIPT!CSESSION::EXECUTE", "");
				dictionary.AddOrUpdate("VBSCRIPT!COLESCRIPT::EXECUTEPENDINGSCRIPTS", "");
				dictionary.AddOrUpdate("VBSCRIPT!COLESCRIPT::SETSCRIPTSTATE", "");
				dictionary.AddOrUpdate("ASP!CACTIVESCRIPTENGINE::TRYCALL", "");
				dictionary.AddOrUpdate("ASP!CACTIVESCRIPTENGINE::CALL", "");
				dictionary.AddOrUpdate("ASP!CALLSCRIPTFUNCTIONOFENGINE", "");
				dictionary.AddOrUpdate("ASP!EXECUTEREQUEST", "");
				dictionary.AddOrUpdate("ASP!EXECUTE", "");
				dictionary.AddOrUpdate("ASP!CHITOBJ::VIPERASYNCCALLBACK", "");
				dictionary.AddOrUpdate("ASP!CVIPERASYNCREQUEST::ONCALL", "");
				dictionary.AddOrUpdate("COMSVCS!CSTAACTIVITYWORK::STAACTIVITYWORKHELPER", "");
				dictionary.AddOrUpdate("OLE32!ENTERFORCALLBACK", "");
				dictionary.AddOrUpdate("OLE32!SWITCHFORCALLBACK", "");
				dictionary.AddOrUpdate("OLE32!PERFORMCALLBACK", "");
				dictionary.AddOrUpdate("OLE32!COBJECTCONTEXT::INTERNALCONTEXTCALLBACK", "");
				dictionary.AddOrUpdate("OLE32!COBJECTCONTEXT::DOCALLBACK", "");
				dictionary.AddOrUpdate("COMBASE!ENTERFORCALLBACK", "");
				dictionary.AddOrUpdate("COMBASE!SWITCHFORCALLBACK", "");
				dictionary.AddOrUpdate("COMBASE!PERFORMCALLBACK", "");
				dictionary.AddOrUpdate("COMBASE!COBJECTCONTEXT::INTERNALCONTEXTCALLBACK", "");
				dictionary.AddOrUpdate("COMBASE!COBJECTCONTEXT::DOCALLBACK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTAACTIVITYWORK::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::PROCESSQUEUEWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::WORKERLOOP", "");
				dictionary.AddOrUpdate("MSVCRT!_THREADSTARTEX", "");
				dictionary.AddOrUpdate("KERNEL32!BASETHREADSTART", "");
				m_boilerPlateFunctionsByOpType.Add(2, dictionary);
				dictionary = new Dictionary<string, string>();
				dictionary.AddOrUpdate("MSVBVM60!EPIINVOKEMETHOD", "");
				dictionary.AddOrUpdate("MSVBVM60!BASIC_CLASS_INVOKE", "");
				dictionary.AddOrUpdate("OLEAUT32!IDISPATCH_INVOKE_STUB", "");
				dictionary.AddOrUpdate("OLEAUT32!IDISPATCH_REMOTEINVOKE_THUNK", "");
				dictionary.AddOrUpdate("RPCRT4!NDRSTUBCALL2", "");
				dictionary.AddOrUpdate("RPCRT4!CSTDSTUBBUFFER_INVOKE", "");
				dictionary.AddOrUpdate("OLEAUT32!CSTUBWRAPPER::INVOKE", "");
				dictionary.AddOrUpdate("OLE32!SYNCSTUBINVOKE", "");
				dictionary.AddOrUpdate("OLE32!STUBINVOKE", "");
				dictionary.AddOrUpdate("OLE32!CCTXCOMCHNL::CONTEXTINVOKE", "");
				dictionary.AddOrUpdate("OLE32!MTAINVOKE", "");
				dictionary.AddOrUpdate("OLE32!STAINVOKE", "");
				dictionary.AddOrUpdate("OLE32!APPINVOKE", "");
				dictionary.AddOrUpdate("OLE32!COMINVOKEWITHLOCKANDIPID", "");
				dictionary.AddOrUpdate("OLE32!COMINVOKE", "");
				dictionary.AddOrUpdate("OLE32!THREADDISPATCH", "");
				dictionary.AddOrUpdate("OLE32!THREADWNDPROC", "");
				dictionary.AddOrUpdate("COMBASE!SYNCSTUBINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!STUBINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!CCTXCOMCHNL::CONTEXTINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!MTAINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!STAINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!APPINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!COMINVOKEWITHLOCKANDIPID", "");
				dictionary.AddOrUpdate("COMBASE!COMINVOKE", "");
				dictionary.AddOrUpdate("COMBASE!THREADDISPATCH", "");
				dictionary.AddOrUpdate("COMBASE!THREADWNDPROC", "");
				dictionary.AddOrUpdate("USER32!INTERNALCALLWINPROC", "");
				dictionary.AddOrUpdate("USER32!USERCALLWINPROCCHECKWOW", "");
				dictionary.AddOrUpdate("USER32!DISPATCHMESSAGEWORKER", "");
				dictionary.AddOrUpdate("USER32!DISPATCHMESSAGEW", "");
				dictionary.AddOrUpdate("COMSVCS!CSTAQUEUELESSMESSAGEWORK::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::DOWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::PROCESSQUEUEWORK", "");
				dictionary.AddOrUpdate("COMSVCS!CSTATHREAD::WORKERLOOP", "");
				dictionary.AddOrUpdate("MSVCRT!_THREADSTARTEX", "");
				dictionary.AddOrUpdate("KERNEL32!BASETHREADSTART", "");
				m_boilerPlateFunctionsByOpType.Add(4, dictionary);
				dictionary = new Dictionary<string, string>();
				dictionary.AddOrUpdate("SYSTEM.RUNTIMEMETHODHANDLE._INVOKEMETHODFAST(SYSTEM.OBJECT, SYSTEM.OBJECT[], SYSTEM.SIGNATURESTRUCT BYREF, SYSTEM.REFLECTION.METHODATTRIBUTES, SYSTEM.RUNTIMETYPEHANDLE)", "");
				dictionary.AddOrUpdate("SYSTEM.RUNTIMEMETHODHANDLE.INVOKEMETHODFAST(SYSTEM.OBJECT, SYSTEM.OBJECT[], SYSTEM.SIGNATURE, SYSTEM.REFLECTION.METHODATTRIBUTES, SYSTEM.RUNTIMETYPEHANDLE)", "");
				dictionary.AddOrUpdate("SYSTEM.REFLECTION.RUNTIMEMETHODINFO.INVOKE(SYSTEM.OBJECT, SYSTEM.REFLECTION.BINDINGFLAGS, SYSTEM.REFLECTION.BINDER, SYSTEM.OBJECT[], SYSTEM.GLOBALIZATION.CULTUREINFO, BOOLEAN)", "");
				dictionary.AddOrUpdate("SYSTEM.REFLECTION.RUNTIMEMETHODINFO.INVOKE(SYSTEM.OBJECT, SYSTEM.REFLECTION.BINDINGFLAGS, SYSTEM.REFLECTION.BINDER, SYSTEM.OBJECT[], SYSTEM.GLOBALIZATION.CULTUREINFO)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.SERVICES.PROTOCOLS.LOGICALMETHODINFO.INVOKE(SYSTEM.OBJECT, SYSTEM.OBJECT[])", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.SERVICES.PROTOCOLS.WEBSERVICEHANDLER.INVOKE()", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.SERVICES.PROTOCOLS.WEBSERVICEHANDLER.COREPROCESSREQUEST()", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.SERVICES.PROTOCOLS.SYNCSESSIONLESSHANDLER.PROCESSREQUEST(SYSTEM.WEB.HTTPCONTEXT)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION.EXECUTESTEP(IEXECUTIONSTEP, BOOLEAN BYREF)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION.SYSTEM.WEB.IHTTPASYNCHANDLER.BEGINPROCESSREQUEST(SYSTEM.WEB.HTTPCONTEXT, SYSTEM.ASYNCCALLBACK, SYSTEM.OBJECT)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPRUNTIME.PROCESSREQUESTINTERNAL(SYSTEM.WEB.HTTPWORKERREQUEST)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPRUNTIME.PROCESSREQUESTNODEMAND(SYSTEM.WEB.HTTPWORKERREQUEST)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HOSTING.ISAPIRUNTIME.PROCESSREQUEST(INTPTR, INT32)", "");
				dictionary.AddOrUpdate("System.Web.Services.Protocols.LogicalMethodInfo.Invoke(System.Object, System.Object[])", "");
				dictionary.AddOrUpdate("System.Web.Services.Protocols.WebServiceHandler.Invoke()", "");
				dictionary.AddOrUpdate("System.Web.Services.Protocols.WebServiceHandler.CoreProcessRequest()", "");
				dictionary.AddOrUpdate("System.Web.Services.Protocols.SyncSessionlessHandler.ProcessRequest(System.Web.HttpContext)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION+APPLICATIONSTEPMANAGER.RESUMESTEPS(SYSTEM.EXCEPTION)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UTIL.CALLIHELPER.EVENTARGFUNCTIONCALLER(INTPTR, SYSTEM.OBJECT, SYSTEM.OBJECT, SYSTEM.EVENTARGS)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.CONTROL.ONLOAD(SYSTEM.EVENTARGS)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.CONTROL.LOADRECURSIVE()", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.PAGE.PROCESSREQUESTMAIN(BOOLEAN, BOOLEAN)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.PAGE.PROCESSREQUEST(BOOLEAN, BOOLEAN)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.PAGE.PROCESSREQUEST()", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.PAGE.PROCESSREQUEST(SYSTEM.WEB.HTTPCONTEXT)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION+PIPELINESTEPMANAGER.RESUMESTEPS(SYSTEM.EXCEPTION)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION.BEGINPROCESSREQUESTNOTIFICATION(SYSTEM.WEB.HTTPCONTEXT, SYSTEM.ASYNCCALLBACK)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPRUNTIME.PROCESSREQUESTNOTIFICATIONPRIVATE(SYSTEM.WEB.HOSTING.IIS7WORKERREQUEST, SYSTEM.WEB.HTTPCONTEXT)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HOSTING.PIPELINERUNTIME.PROCESSREQUESTNOTIFICATIONHELPER(INTPTR, INTPTR, INTPTR, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HOSTING.PIPELINERUNTIME.PROCESSREQUESTNOTIFICATION(INTPTR, INTPTR, INTPTR, INT32)", "");
				dictionary.AddOrUpdate("DOMAINNEUTRALILSTUBCLASS.IL_STUB_REVERSEPINVOKE(INT64, INT64, INT64, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HOSTING.UNSAFEIISMETHODS.MGDINDICATECOMPLETION(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUS BYREF)", "");
				dictionary.AddOrUpdate("DOMAINNEUTRALILSTUBCLASS.IL_STUB_PINVOKE(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUS BYREF)", "");
				dictionary.AddOrUpdate("DOMAINNEUTRALILSTUBCLASS.IL_STUB_COMTOCLR(INT64, INT32, INT32 BYREF)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UTIL.CALLIEVENTHANDLERDELEGATEPROXY.CALLBACK(SYSTEM.OBJECT, SYSTEM.EVENTARGS)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HTTPAPPLICATION+CALLHANDLEREXECUTIONSTEP.SYSTEM.WEB.HTTPAPPLICATION.IEXECUTIONSTEP.EXECUTE()", "");
				dictionary.AddOrUpdate("DOMAINNEUTRALILSTUBCLASS.IL_STUB(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUS BYREF)", "");
				dictionary.AddOrUpdate("DOMAINNEUTRALILSTUBCLASS.IL_STUB(INT64, INT64, INT64, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.UI.PAGE.PROCESSREQUESTWITHNOASSERT(SYSTEM.WEB.HTTPCONTEXT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HOSTING.PIPELINERUNTIME.PROCESSREQUESTNOTIFICATIONHELPER(INTPTR, INTPTR, INTPTR, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HOSTING.PIPELINERUNTIME.PROCESSREQUESTNOTIFICATION(INTPTR, INTPTR, INTPTR, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!DOMAINNEUTRALILSTUBCLASS.IL_STUB_REVERSEPINVOKE(INT64, INT64, INT64, INT32)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION.EXECUTESTEP(IEXECUTIONSTEP, BOOLEAN BYREF)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION.BEGINPROCESSREQUESTNOTIFICATION(SYSTEM.WEB.HTTPCONTEXT, SYSTEM.ASYNCCALLBACK)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPRUNTIME.PROCESSREQUESTNOTIFICATIONPRIVATE(SYSTEM.WEB.HOSTING.IIS7WORKERREQUEST, SYSTEM.WEB.HTTPCONTEXT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!DOMAINNEUTRALILSTUBCLASS.IL_STUB_PINVOKE(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUS BYREF)", "");
				dictionary.AddOrUpdate("[[INLINEDCALLFRAME] (SYSTEM.WEB.HOSTING.UNSAFEIISMETHODS.MGDINDICATECOMPLETION)] SYSTEM.WEB.HOSTING.UNSAFEIISMETHODS.MGDINDICATECOMPLETION(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUSBYREF", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.HOSTING.UNSAFEIISMETHODS.MGDINDICATECOMPLETION(INTPTR, SYSTEM.WEB.REQUESTNOTIFICATIONSTATUSBYREF)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCCONTROLLERACTIONINVOKER", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCCONTROLLERACTIONINVOKER.ENDINVOKEACTION(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.CONTROLLER.B__1D(SYSTEM.IASYNCRESULT, EXECUTECORESTATE)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.CONTROLLER.ENDEXECUTECORE(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.CONTROLLER.ENDEXECUTE(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.MVCHANDLER.B__5(SYSTEM.IASYNCRESULT, PROCESSREQUESTSTATE)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.MVCHANDLER.ENDPROCESSREQUEST(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCCONTROLLERACTIONINVOKER.ENDINVOKEACTIONMETHODWITHFILTERS(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.CONTROLLERACTIONINVOKER.INVOKEACTIONMETHOD(SYSTEM.WEB.MVC.CONTROLLERCONTEXT, SYSTEM.WEB.MVC.ACTIONDESCRIPTOR, SYSTEM.COLLECTIONS.GENERIC.IDICTIONARY`2M.OBJECT>)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCCONTROLLERACTIONINVOKER.>B__39(SYSTEM.IASYNCRESULT, ACTIONINVOCATION)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCCONTROLLERACTIONINVOKER.ENDINVOKEACTIONMETHOD(SYSTEM.IASYNCRESULT)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.REFLECTEDACTIONDESCRIPTOR.EXECUTE(SYSTEM.WEB.MVC.CONTROLLERCONTEXT, SYSTEM.COLLECTIONS.GENERIC.IDICTIONARY`2)", "");
				dictionary.AddOrUpdate("SYSTEM.WEB.MVC.ASYNC.ASYNCRESULTWRAPPER", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION.BEGINPROCESSREQUESTNOTIFICATION(SYSTEM.WEB.HTTPCONTEXT, SYSTEM.ASYNCCALLBACK)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION.EXECUTESTEP(IEXECUTIONSTEP, BOOLEAN BYREF)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION", "");
				dictionary.AddOrUpdate("SYSTEM_WEB!SYSTEM.WEB.HTTPAPPLICATION.EXECUTESTEPIMPL(IEXECUTIONSTEP)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.CONTROLLERACTIONINVOKER.INVOKEACTIONMETHOD(SYSTEM.WEB.MVC.CONTROLLERCONTEXT, SYSTEM.WEB.MVC.ACTIONDESCRIPTOR, SYSTEM.COLLECTIONS.GENERIC.IDICTIONARY`2)", "");
				dictionary.AddOrUpdate("SYSTEM_WEB_MVC!SYSTEM.WEB.MVC.ASYNC.ASYNCRESULTWRAPPER", "");
				m_boilerPlateFunctionsByOpType.Add(3, dictionary);
				dictionary = m_boilerPlateFunctionsByOpType[1];
				dictionary.AddOrUpdate("[[CONTEXTTRANSITIONFRAME]]", "");
				dictionary.AddOrUpdate("UNKNOW", "");
				dictionary.AddOrUpdate("UNKNOWN", "");
				dictionary.AddOrUpdate("[[STUBHELPERFRAME]]", "");
				dictionary.AddOrUpdate("[[DEBUGGERU2MCATCHHANDLERFRAME]]", "");
				dictionary.AddOrUpdate("[[GCFRAME]]", "");
				dictionary.AddOrUpdate("SYSTEM_DATA!DOMAINNEUTRALILSTUBCLASS.IL_STUB_PINVOKE(SNI_CONNWRAPPER *, SNI_PACKET * *, INT32)", "");
			}
			return m_boilerPlateFunctionsByOpType;
		}
	}

	public Dictionary<int, string> TypeStrings
	{
		get
		{
			if (m_TypeStrings.Count == 0)
			{
				m_TypeStrings.Add(2, "ASP Request");
				m_TypeStrings.Add(3, "ASP.NET Request");
				m_TypeStrings.Add(4, "Inbound COM Call");
				m_TypeStrings.Add(1, "Unknown Operation");
			}
			return m_TypeStrings;
		}
	}

	public Dictionary<string, COperation> OperationsByDuration => m_operationsByDuration;

	public Dictionary<string, COperation> OperationsByAvgCpu => m_OperationsByAvgCpu;

	public Dictionary<string, COperation> OperationsByMaxCpu => m_OperationsByMaxCpu;

	public int Count => m_operationsByDuration.Count;

	public COperations()
	{
		m_operationsByDuration = new Dictionary<string, COperation>();
		m_OperationsByAvgCpu = new Dictionary<string, COperation>();
		m_OperationsByMaxCpu = new Dictionary<string, COperation>();
		m_OperationsByIncrCpu = new Dictionary<string, COperation>();
		m_TypeStrings = new Dictionary<int, string>();
		m_FunctionsByHitCount = new Dictionary<string, int[]>();
		m_ValueType = 0;
	}

	public void LoadTopXFromSortedList(Dictionary<string, COperation> dict, bool excludeUnknownOps, int valueType)
	{
		COperation cOperation = null;
		m_ValueType = valueType;
		foreach (string key in dict.Keys)
		{
			if (m_operationsByDuration.Count >= 10000)
			{
				break;
			}
			cOperation = dict[key];
			if ((excludeUnknownOps && cOperation.OpType <= 1) || cOperation.ValueByValueType(m_ValueType) < (double)Globals.TopOperationsThresholds[m_ValueType])
			{
				break;
			}
			m_operationsByDuration.Add(key, cOperation);
		}
	}

	public void ShowStatsEx(string title, string tip, bool collapsed, int key)
	{
		ShowStatsEx(title, tip, collapsed, key.ToString());
	}

	public void ShowStatsEx(string title, string tip, bool collapsed, string key)
	{
		COperation cOperation = null;
		GraphRow graphRow = null;
		Dictionary<int, GraphRow> dictionary = null;
		double num = 0.0;
		if (m_operationsByDuration.Count <= 0)
		{
			return;
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection(key, (SectionType)0);
		val.Title = title;
		val.Collapsible = true;
		Globals.Manager.CurrentSection = val;
		if (tip != "")
		{
			Globals.Manager.Write("<BR><BR><font size=4 color='Green'><i><u>Tip</u>:&nbsp;&nbsp;</i></font>" + Convert.ToString(tip) + "<BR><BR>");
		}
		if (m_ValueType > 0)
		{
			dictionary = new Dictionary<int, GraphRow>();
			foreach (string key2 in m_operationsByDuration.Keys)
			{
				cOperation = m_operationsByDuration[key2];
				graphRow = new GraphRow();
				graphRow.Caption = Convert.ToString(cOperation.Name) + " - " + PerfFunctions.FormatFunctionName(cOperation.TopFunctionName, bChop: true, this);
				graphRow.Link = "#Operation:" + Convert.ToString(cOperation.Key);
				graphRow.OnClick = "javascript:doToggle3(document.all(\"PerformanceDetails-t\"), true);return true;";
				num = (graphRow.Value = cOperation.ValueByValueType(m_ValueType));
				if (m_ValueType == 4)
				{
					graphRow.Caption2 = Globals.HelperFunctions.PrintTime(num);
				}
				else
				{
					graphRow.Caption2 = Convert.ToString(num) + "%";
				}
				dictionary.Add(dictionary.Count, graphRow);
			}
			BarGraph barGraph = new BarGraph();
			barGraph.InitFromDict(dictionary);
			barGraph.DrawGraph();
		}
		ShowFunctionStats(title, 0);
		Globals.Manager.Write("<br><br>");
		ShowFunctionStats(title, 1);
		Globals.Manager.CurrentSection = val.Parent;
	}

	private void ShowFunctionStats(object title, int functionType)
	{
		BarGraph barGraph = null;
		int num = 0;
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		Dictionary<string, int[]> dictionary = null;
		string text5 = "";
		string text6 = "";
		string text7 = "";
		if (FunctionsByHitCount.Count == 0)
		{
			return;
		}
		switch (functionType)
		{
		default:
			return;
		case 0:
			text5 = "<h3>";
			text6 = "</h3>";
			text2 = "user";
			text4 = Convert.ToString(Globals.HelperFunctions.Spaces(6)) + "(excludes system functions)";
			num3 = 3;
			num4 = 100;
			break;
		case 1:
			text5 = "<b>";
			text6 = "</b>";
			text2 = "";
			text4 = Convert.ToString(Globals.HelperFunctions.Spaces(6)) + "(includes system functions, excludes boiler-plate functions)";
			num3 = 2;
			num4 = 100;
			break;
		}
		if (Equals(Globals.g_AllOperations))
		{
			text3 = "all operations";
		}
		else
		{
			text7 = "operation";
			if (Convert.ToString(title).GetSafeLength() >= 16 && Convert.ToString(title).Substring(0, 3) == "Top" && Convert.ToString(title).IndexOf("Threads By") >= 1)
			{
				text7 = "thread";
			}
			text3 = ((Convert.ToInt32(Count) != 1) ? ("these " + text7 + "s") : ("this " + text7));
		}
		dictionary = FunctionsByHitCount;
		foreach (string key in dictionary.Keys)
		{
			int[] array = dictionary[key];
			num2 = array[0];
			if (array[1] >= num3)
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		if (num < num4)
		{
			num4 = num;
			text = "All ";
		}
		else
		{
			text = "Top " + Convert.ToString(num4) + " ";
		}
		Globals.Manager.Write(text5 + text + text2 + " functions in " + text3 + text4 + text6);
		barGraph = new BarGraph();
		barGraph.SetRowCount(num4);
		num = 0;
		foreach (string key2 in dictionary.Keys)
		{
			int[] array2 = dictionary[key2];
			num2 = array2[0];
			if (array2[1] >= num3)
			{
				barGraph.Rows[num].Caption = PerfFunctions.FormatFunctionName(key2, bChop: true, this);
				barGraph.Rows[num].Value = num2;
				barGraph.Rows[num].Caption2 = Convert.ToString(num2) + " hits";
				num++;
				if (num >= num4)
				{
					break;
				}
			}
		}
		barGraph.DrawGraph();
		Globals.Manager.Write("<br>");
	}

	public void RollupFunctionsByHitCount()
	{
		Dictionary<string, int[]> dictionary = null;
		int num = 0;
		int num2 = 0;
		int[] array = null;
		int num3 = 0;
		if (m_operationsByDuration.Count <= 0)
		{
			return;
		}
		foreach (string key in m_operationsByDuration.Keys)
		{
			dictionary = m_operationsByDuration[key].FunctionsByHitCount;
			foreach (string key2 in dictionary.Keys)
			{
				array = dictionary[key2];
				num = array[0];
				num2 = array[1];
				if (m_FunctionsByHitCount.ContainsKey(key2))
				{
					array = m_FunctionsByHitCount[key2];
					num3 = num + array[0];
					m_FunctionsByHitCount[key2] = new int[2] { num3, num2 };
				}
				else
				{
					num3 = num;
					m_FunctionsByHitCount.Add(key2, new int[2] { num3, num2 });
				}
			}
		}
		SortFunctionsByHitCountAndSetWatermark();
	}

	private void SortFunctionsByHitCountAndSetWatermark()
	{
		Dictionary<string, int[]> dictionary = null;
		Dictionary<string, int[]> dictionary2 = null;
		int num = 0;
		string text = "";
		int num2 = 0;
		bool flag = false;
		int[] array = null;
		int num3 = 0;
		if (m_FunctionsByHitCount.Count <= 1)
		{
			return;
		}
		dictionary2 = m_FunctionsByHitCount;
		dictionary = new Dictionary<string, int[]>();
		while (dictionary2.Count > 0)
		{
			text = "-1";
			foreach (string key in dictionary2.Keys)
			{
				array = dictionary2[key];
				num = array[0];
				flag = false;
				if (text == "-1")
				{
					flag = true;
				}
				else if (num > num2)
				{
					flag = true;
				}
				if (flag)
				{
					num2 = num;
					text = key;
				}
			}
			array = dictionary2[text];
			dictionary.Add(Convert.ToString(text), array);
			if (array[1] == 3 && num3 < 10)
			{
				num3++;
				m_TopUserFunctionWatermark = num2;
			}
			dictionary2.Remove(Convert.ToString(text));
		}
		m_FunctionsByHitCount = dictionary;
	}

	public void Sort()
	{
		Dictionary<string, COperation> dictionary = null;
		Dictionary<string, COperation> dictionary2 = null;
		Dictionary<string, COperation> dictionary3 = null;
		double num = 0.0;
		double num2 = 0.0;
		string text = "";
		int num3 = 0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		int num8 = 0;
		double num9 = 0.0;
		double num10 = 0.0;
		COperation cOperation = null;
		bool flag = false;
		if (m_operationsByDuration.Count <= 1 || m_OperationsByAvgCpu.Count != 0)
		{
			return;
		}
		dictionary2 = m_operationsByDuration;
		dictionary = new Dictionary<string, COperation>();
		dictionary3 = new Dictionary<string, COperation>();
		while (dictionary2.Count > 0)
		{
			text = "-1";
			foreach (string key in dictionary2.Keys)
			{
				cOperation = dictionary2[key];
				num8 = cOperation.OpType;
				num6 = cOperation.DurationMin;
				num7 = cOperation.DurationMax;
				flag = false;
				if (text == "-1")
				{
					flag = true;
				}
				else if (key != text)
				{
					if (num3 == 1 && num8 != 1)
					{
						flag = true;
					}
					else if (num3 != 1 && num8 == 1)
					{
						flag = false;
					}
					else if (num6 > num)
					{
						flag = true;
					}
					else if (num6 == num && num7 > num2)
					{
						flag = true;
					}
				}
				if (flag)
				{
					text = key;
					num3 = num8;
					num = num6;
					num2 = num7;
				}
			}
			cOperation = dictionary2[text];
			dictionary.Add(text, cOperation);
			dictionary3.Add(text, cOperation);
			dictionary2.Remove(text);
		}
		m_operationsByDuration = dictionary;
		dictionary2 = dictionary3;
		dictionary = new Dictionary<string, COperation>();
		dictionary3 = new Dictionary<string, COperation>();
		while (dictionary2.Count > 0)
		{
			text = "-1";
			foreach (string key2 in dictionary2.Keys)
			{
				cOperation = dictionary2[key2];
				num9 = cOperation.AvgCPU;
				flag = false;
				if (text == "-1")
				{
					flag = true;
				}
				else if (key2 != text && num9 > num4)
				{
					flag = true;
				}
				if (flag)
				{
					text = key2;
					num4 = num9;
				}
			}
			cOperation = dictionary2[text];
			dictionary.Add(text, cOperation);
			dictionary3.Add(text, cOperation);
			dictionary2.Remove(text);
		}
		m_OperationsByAvgCpu = dictionary;
		dictionary2 = dictionary3;
		dictionary = new Dictionary<string, COperation>();
		while (dictionary2.Count > 0)
		{
			text = "-1";
			foreach (string key3 in dictionary2.Keys)
			{
				cOperation = dictionary2[key3];
				num10 = cOperation.MaxCPU;
				flag = false;
				if (text == "-1")
				{
					flag = true;
				}
				else if (key3 != text && num10 > num5)
				{
					flag = true;
				}
				if (flag)
				{
					text = key3;
					num5 = num10;
				}
			}
			cOperation = dictionary2[text];
			dictionary.Add(text, cOperation);
			dictionary2.Remove(text);
		}
		m_OperationsByMaxCpu = dictionary;
	}

	public void AddOperation(COperation operation, int dumpNumber)
	{
		string text = null;
		text = operation.Key;
		if (m_operationsByDuration.ContainsKey(text))
		{
			operation = m_operationsByDuration[text].WasFoundAgain(operation, dumpNumber);
		}
		else
		{
			m_operationsByDuration.Add(text, operation);
		}
	}

	public bool IsTopUserFrame(CRelevantStackFrame relevantStackFrame)
	{
		return IsTopUserFunction(relevantStackFrame.FunctionNameNoOffset);
	}

	public bool IsTopUserFunction(string fnName)
	{
		bool result = false;
		int[] array = null;
		if (m_FunctionsByHitCount.ContainsKey(fnName))
		{
			array = m_FunctionsByHitCount[fnName];
			if (array[0] >= m_TopUserFunctionWatermark)
			{
				result = true;
			}
		}
		return result;
	}
}
