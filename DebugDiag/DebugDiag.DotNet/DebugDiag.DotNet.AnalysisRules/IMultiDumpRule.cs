namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// This type of analysis rule runs against all dumps selected for analysis.  Use this interface 
/// only if you need to correlate/aggregate info from multiple dumps together in one piece of analysis code (rare),
/// for example to show a trend of memory usage over time using multiple dumps of the same process.
/// </summary>
public interface IMultiDumpRule : IAnalysisRuleBase
{
	/// <summary>
	/// Main method executed when the analysis rule starts.
	/// </summary>
	/// <param name="manager">Used by all analysis rule interfaces.  Allows rules to write results to the report file.  
	/// Also provides access to all the dumps and debuggers for processing multiple dumps in an IMultiDumpRule.</param>
	/// <param name="progress">Access to Progress reporting features useful for displaying the progress of an analysis to users</param>
	void RunAnalysisRule(NetScriptManager manager, NetProgress progress);
}
