using System;
using System.Collections.Generic;
using System.ServiceModel;
using DebugDiag.DotNet.AnalysisRules;

namespace DebugDiag.DotNet.x86Analysis;

[ServiceContract(CallbackContract = typeof(IAnalysisServiceProgress))]
internal interface IAnalysisService
{
	[OperationContract]
	[FaultContract(typeof(AnalysisTimeoutException))]
	NetResults RunAnalysisRules(List<AnalysisRuleInfo> analysisRuleInfos, List<string> dumpFiles, string symbolPath, string imagePath, string reportFileFullPath, TimeSpan timeout, bool twoTabs, bool includeSourceAndLineInformationInAnalysisReports, bool setContextOnCrashDumps, bool doHangAnalysisOnCrashDumps, bool includeHttpHeadersInClientConns, bool groupIdenticalStacks, bool includeInstructionPointerInAnalysisReports, out List<object> facts);
}
