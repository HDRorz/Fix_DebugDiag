using ComplusDDExt;

namespace DebugDiag.AnalysisRules;

public interface IAnalyzeActivities
{
	string GetActivityWithLink(IDbgActivity activity);

	string GetActivityWithLinkRoot(IDbgActivity activity);

	string GetActivityData(IDbgActivity activity);

	void ReportAllActivities();

	void ReportAllSimpleDeadlockedActivities();

	void ReportAlBlockinglNonDeadlockedActivities();

	void ReportAllTransitioningActivities();

	void SortAllActivities();

	string GetRecommendationForTransitioningActivity(IDbgActivity activity);

	string GetRecommendationForBlockingActivity(IDbgActivity activity);

	string GetRecommendationForOrphanedActivity(CDbgActivity activity);

	string GetRecommendationForDeadlockedActivity(IDbgActivity activity);
}
