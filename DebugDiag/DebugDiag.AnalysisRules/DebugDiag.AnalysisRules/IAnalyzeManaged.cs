namespace DebugDiag.AnalysisRules;

public interface IAnalyzeManaged
{
	string GetTebAsHex(CacheFunctions.ScriptThreadClass Thread);

	void InitClrGlobals(bool doHeapScans = false);

	void ReportAllExceptions();

	string[] DumpAllExceptions();

	void DisplayBangThreads();

	void DisplayBangThreadPool();

	void CheckWebEngineQueue();

	void DisplayWebEngineQueueMessage(string currentRequests, string MaxActiveRequests, int QueueCount);

	void CheckThreadPool();

	void AnalyzeBlockedFinalizer();

	void FindSyncBlk();

	string IsWaitingOnSyncblk(int threadid);

	string FindOwnerForSyncblk(object syncBlk);

	bool IsWebApp();

	bool IsWcfServiceHost();

	bool IsWcfClient();

	bool IsIIS7App();

	string GetManagedExceptionType(object ExceptionObjHexAddr, bool bInnerException);

	string GetManagedExceptionMsg(object ExceptionObjHexAddr, bool bInnerException);

	object getManagedObjectType(object objHexAddr);

	object GetManagedIp2md(string ipValInHex);

	void PrintContextHeader();

	void PrintContextFooter();

	void PrintHttpContextInfo(object context, bool boolHeader);

	string GetThreadForHttpContext(object context);

	void PrintHttpContextInformation();

	string regexReplace(object aSrcStr, string aPattern, string aReplacement);

	void GetGarbageCollectionInformation();

	void FindCLRKnownThreads();

	void AddExceptionsFromBangThreads(int intThreadId, string exceptionObject);

	void FindDebugTrue();

	bool IsStaticBoolTrueInAnyAppDomain(string objectAddress, string variable);

	void AnalyzeShuttingHttpRuntime(object httpRunTime);

	bool WildCardMatchTypeName(object inputStr, object typeString);

	string[] DumpHeapForType(string ObjectType);

	string normalizeWhitespace(string stringToTrim);

	object getUriString(object aObjAddr);

	void FindManagedExceptionsForAllThreads();

	void PopulateDSOCache();

	void PopulateDSOCacheForThread(int intThreadId);

	void findManagedExceptions(int intThreadId);

	void displayManagedExceptions();

	string GetStackTraceFromException(string exceptionObject);

	string HTMLEncode(object sVal);

	bool IsLowerRuntimeVersion(object versionNumber);

	void FindCLRDLL();

	void LoadCLRInformation(bool ignoreMiniDumpFailure = false);

	void GetClrVersion(ref int Major, ref int Minor);

	bool IsClrExtensionExecuting();

	bool IsClrLoaded();

	bool IsClrV2();

	CacheFunctions.ScriptModuleClass GetClrModule();

	void CheckClrExtension(bool ignoreMiniDumpFailure = false);

	void AnalyzeWCFServiceHost();

	void GetFlowThrottleValues(object serviceThrottle, string flowThrottlePropName, ref int count, ref int capacity, ref double pct);

	string GetTypeNameFromSystemType(object objectAddress);

	string DumpObject(object objectAddress, string fieldName);

	string DumpString(object objectAddress, string fieldName);

	object DumpStringVal(object stringAddress);

	int DumpShort(object objectAddress, string fieldName);

	object DumpByte(object objectAddress, string fieldName);

	object DumpLong(object objectAddress, string fieldName);

	object DumpQuad(object objectAddress, string fieldName);

	string DumpDateTimeAsString(object dateTimeAddress);

	object[] DumpObjectArrayRaw(object objectString);

	object[] DumpObjectArray(object objectAddress, string fieldName);

	void AnalyzeWCFRequest();

	void GetWCFThreadsInformation();

	AnalyzedThreadClass getDotNetAnalysis(CacheFunctions.ScriptThreadClass Thread, object FrameID);

	string ParseChainOutput();

	string FindObjectFromDSO(string objectType, int intThreadId, object stopOnFirstObject);

	AnalyzedThreadClass RandomKnownIssues(CacheFunctions.ScriptThreadClass Thread, object FrameID);

	void GenerateADODotNetReport();

	void CheckForWellKnownExceptionTypes(object ExceptionType, object ExceptionMessage, object ExceptionStack, object ExceptionStackRemote, object ExceptionCount);

	bool IsWellKnownUnhandledException(object ExceptionType, object ExceptionMessage, object stackTrace, object remoteStackTraceString, CacheFunctions.ScriptThreadClass ExceptionThread, ref string AdditionalDescriptionString, ref string RecommendationString, ref string SolutionSourceID);

	string[] DumpArrayElements(object arrayAddress, string fieldName, object className);

	void PrintDebugModuleInformation();

	void CheckBufferedSystemDotNetConnections();

	void CheckLeakedSystemDotNetConnections();

	string GetOwnerIdForHttpApplicationStateLock(CacheFunctions.ScriptThreadClass Thread);

	bool CheckInClrStackIfSleepIsCozMSFT(CacheFunctions.ScriptThreadClass Thread);

	string GetManagedExceptionPtr(CacheFunctions.ScriptThreadClass ExceptionThread);
}
