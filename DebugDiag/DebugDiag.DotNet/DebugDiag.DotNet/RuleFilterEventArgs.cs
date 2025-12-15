using System;
using System.Collections.Generic;

namespace DebugDiag.DotNet;

/// <summary>
/// Provides data for the <c>OnRuleFilter</c> Event
/// </summary>
public class RuleFilterEventArgs : EventArgs
{
	/// <summary>
	/// Field containing the Filter result.
	/// </summary>
	public RuleFilterResults FilterResult;

	/// <summary>
	/// Field containing the Analysis rule name.
	/// </summary>
	public string AnalysisRuleName;

	/// <summary>
	/// Field containing a List of strings containing all the Dump file names.
	/// </summary>
	public List<string> DumpFiles;

	/// <summary>
	/// Initializes a new instance of <c>RuleFilterEventArgs</c> class.
	/// </summary>
	/// <param name="analysisRuleName">The name of the rule</param>
	/// <param name="dumpFileFullPath">Full path and dump file name</param>
	/// <param name="filterResult">Filter result</param>
	/// <overloads>This constructor has one overload</overloads>
	public RuleFilterEventArgs(string analysisRuleName, string dumpFileFullPath, RuleFilterResults filterResult)
	{
		AnalysisRuleName = analysisRuleName;
		FilterResult = filterResult;
		DumpFiles = new List<string>();
		DumpFiles.Add(dumpFileFullPath);
	}

	/// <summary>
	/// Initializes a new instance of <c>RuleFilterEventArgs</c> class.
	/// </summary>
	/// <param name="analysisRuleName">The name of the rule</param>
	/// <param name="dumpFiles">A list of strings containing the full names of the dump files.</param>
	/// <param name="filterResult">Filter result</param>
	public RuleFilterEventArgs(string analysisRuleName, List<string> dumpFiles, RuleFilterResults filterResult)
	{
		AnalysisRuleName = analysisRuleName;
		FilterResult = filterResult;
		DumpFiles = dumpFiles;
	}
}
