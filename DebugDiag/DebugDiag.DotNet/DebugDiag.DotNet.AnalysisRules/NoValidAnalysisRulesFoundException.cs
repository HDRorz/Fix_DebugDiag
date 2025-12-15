using System;

namespace DebugDiag.DotNet.AnalysisRules;

/// <summary>
/// Exception thrown whenever a type of dump has no Analysis rules that can be executed to analyze it.
/// </summary>
public class NoValidAnalysisRulesFoundException : Exception
{
	/// <summary>
	/// Default Constructor
	/// </summary>
	/// <param name="message">Exception message</param>
	public NoValidAnalysisRulesFoundException(string message)
		: base(message)
	{
	}
}
