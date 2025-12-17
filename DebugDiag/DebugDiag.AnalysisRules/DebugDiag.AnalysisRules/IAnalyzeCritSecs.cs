namespace DebugDiag.AnalysisRules;

public interface IAnalyzeCritSecs
{
	string GetPreviousExceptionsRecommendationStringForOwnedLock(string nThreadNum, string sLockModuleName);

	string GetPreviousExceptionsRecommendationStringForOrphanedLock(object sLockModuleName);

	void AnalyzeRootLocks();

	void AnalyzeCritSecs();

	void ReportCritSecs();
}
