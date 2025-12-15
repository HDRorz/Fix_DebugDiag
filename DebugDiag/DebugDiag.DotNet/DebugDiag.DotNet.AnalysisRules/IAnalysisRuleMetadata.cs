namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// Use this optional interface to add a category and description for the rule, to be displayed in the UI
/// </summary>
public interface IAnalysisRuleMetadata
{
	/// <summary>
	/// Provides access to the category where the rule belongs. Examples Crash/Hang Analyzers, Performance Analyzers, etc...
	/// </summary>
	string Category { get; }

	/// <summary>
	/// Provides access to the description for the Analysis Rule that appears on the Analyzer UI.
	/// </summary>
	string Description { get; }
}
