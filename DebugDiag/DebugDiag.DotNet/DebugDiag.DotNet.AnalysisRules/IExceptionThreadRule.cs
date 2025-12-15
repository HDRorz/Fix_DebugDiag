using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// this type of analysis rule only runs against the exception thread in a crash dump
/// </summary>
public interface IExceptionThreadRule : IAnalysisRuleBase
{
	/// <summary>
	/// Main method executed when the analysis rule starts.
	/// </summary>
	/// <param name="manager">Used by all analysis rule interfaces. Allows rules to write results to the report file.</param>
	/// <param name="debugger">This is the entry point which exposes all the underlying debugger functionality for a given dump file.  
	/// One NetDbgObj instance “points to” one dump file.</param>
	/// <param name="exceptionThread">Provides access to the thread information where the exception was thrown.</param>
	/// <param name="nativeException">Provides access to the native Exception object that has been thrown on the thread.</param>
	/// <param name="managedException">Provides access to the managed Exception object that has been thrown on the thread if any.</param>
	/// <param name="progress">Access to Progress reporting features useful for displaying the progress of an analysis to users.</param>
	void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetDbgThread exceptionThread, NetDbgException nativeException, ClrException managedException, NetProgress progress);
}
