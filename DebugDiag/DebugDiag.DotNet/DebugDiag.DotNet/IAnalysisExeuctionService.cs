using System.ServiceModel;

namespace DebugDiag.DotNet;

[ServiceContract]
internal interface IAnalysisExeuctionService
{
	[OperationContract(IsOneWay = true)]
	void LogRuleExecution(string[] ruleNames, string[] errorMessages, string[] dumpFiles);
}
