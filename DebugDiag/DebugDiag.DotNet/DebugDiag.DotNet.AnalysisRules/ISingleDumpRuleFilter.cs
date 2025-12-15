namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
///
/// </summary>
public interface ISingleDumpRuleFilter
{
	bool ShouldRunAnalysis(NetDbgObj debugger, AnalysisModes mode, ref string filterReason);
}
