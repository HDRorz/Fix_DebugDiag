using System;

namespace DebugDiag.DotNet;

/// <summary>
/// Provides data for the <c>PreExecuteRule</c> Event
/// </summary>
public class RuleExecutionEventArgs : EventArgs
{
	/// <summary>
	/// Field containing the RuleName
	/// </summary>
	public string AnalysisRuleName;

	/// <summary>
	/// Field containing the Dump Name if the rule is not Multidump, otherwise will be empty
	/// </summary>
	public string DumpName;

	/// <summary>
	/// Field containing the thread ID for rules executed on each thread, otherwise will be empty
	/// </summary>
	public string ThreadID;

	/// <summary>
	/// Default Constructor
	/// </summary>
	/// <param name="analisysRuleName">Name of the Analysis Rule</param>
	/// <param name="dumpName">Name of the dump where the rule will be executed if not a Multidump rule</param>
	/// <param name="threadID">ID of the thread where the rule will be executed if a Thread rule</param>
	public RuleExecutionEventArgs(string analisysRuleName, string dumpName, string threadID)
	{
		AnalysisRuleName = analisysRuleName;
		DumpName = dumpName;
		ThreadID = threadID;
	}
}
