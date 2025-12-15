namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
///
/// </summary>
public interface IMultiDumpRuleFilter
{
	bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason);
}
