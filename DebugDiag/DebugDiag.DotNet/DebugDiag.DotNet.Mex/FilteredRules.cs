using System;
using System.Collections.Generic;

namespace DebugDiag.DotNet.Mex;

internal class FilteredRules : Dictionary<Type, FilteredRule>
{
	public FilteredRules(dynamic realFilteredRulesList)
	{
		foreach (dynamic realFilteredRules in realFilteredRulesList)
		{
			Add(realFilteredRules.Type, new FilteredRule(realFilteredRules));
		}
	}
}
