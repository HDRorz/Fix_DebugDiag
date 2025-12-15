namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// This type of analysis rule runs against 1 dump at a time.  By default it runs only against *hang* dumps.
/// The user can opt to run this type of hang analysis against all *crash* dumps as well.
/// </summary>
public interface IHangDumpRule : IAnalysisRuleBase
{
	/// <summary>
	/// Main method executed when the analysis rule starts.
	/// </summary>
	/// <param name="manager">Used by all analysis rule interfaces.  Allows rules to write results to the report file.</param>
	/// <param name="debugger">This is the entry point which exposes all the underlying debugger functionality for a given dump file.  
	/// One NetDbgObj instance “points to” one dump file.</param>
	/// <param name="progress">Access to Progress reporting features useful for displaying the progress of an analysis to users.</param>
	void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetProgress progress);
}
