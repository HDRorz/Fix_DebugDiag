using IISInfoLib;

namespace DebugDiag.AnalysisRules;

internal interface IReportASPInfo
{
	bool IsASPDebuggingEnabled();

	void Report_ASPInfo();

	int FileWithMorethan10Includes();

	string GetTemplateCacheSize(IASPInfo ASPInfo);

	void OutputVBScriptStack(IASPRequest ASPRequest);

	void ReportASPRequest(IASPRequest ASPRequest);
}
