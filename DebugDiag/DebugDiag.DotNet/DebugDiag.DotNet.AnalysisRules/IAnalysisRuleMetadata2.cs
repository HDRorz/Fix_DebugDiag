namespace DebugDiag.DotNet.AnalysisRules;

public interface IAnalysisRuleMetadata2 : IAnalysisRuleMetadata
{
	/// <summary>
	/// Provides access to the description for the Analysis Rule that appears on the Analyzer UI.
	/// </summary>
	bool SupportsKernelDumps { get; }

	/// <summary>
	/// Provides access to the description for the Analysis Rule that appears on the Analyzer UI.
	/// </summary>
	bool SupportsUserDumps { get; }
}
