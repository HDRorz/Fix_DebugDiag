using IISInfoLib;

namespace DebugDiag.AnalysisRules;

internal interface IReportHTTPInfo
{
	void Report_HTTPInfo();

	void ReportLongRunningRequests();

	void ReportClientConnection(IClientConnection ClientConn);
}
