namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// Enumeration of possible Analysis modes
/// </summary>
public enum AnalysisModes
{
	/// <summary>
	/// Analysis is being performed with the DebugDiag Analysis UI
	/// </summary>
	Interactive,
	/// <summary>
	/// Analysis is being performed by an unattended analysis engine
	/// </summary>
	Unattended
}
