namespace DebugDiag.AnalysisRules;

public interface IAnalyzeComPlus
{
	int GetCurrentSTAThreadCount();

	long GetCOMPlusInstanceCount();

	bool IsAnyWellKnownCOMSTALoaded();

	string GetSTAThreadPoolReportLink();

	string GetCOMPlusComponentStatsLink();

	string GetCOMSTAReportLink();

	void AnalyzeComAndComPlusInfo();

	void ReportCOMPlusInfo();

	void ReportCOMInfo();

	bool IsWellKnownCOMSTA(int systemTID);

	int CountAppInvokeFrames(CacheFunctions.ScriptThreadClass Thread, int nStopAt);

	int GetHostApartmentID(string sHostPrefix);
}
