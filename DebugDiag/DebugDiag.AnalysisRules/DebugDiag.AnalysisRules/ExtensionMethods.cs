using System.Collections.Generic;

namespace DebugDiag.AnalysisRules;

public static class ExtensionMethods
{
	public static int GetSafeLength(this string thisobj)
	{
		if (string.IsNullOrEmpty(thisobj))
		{
			return 0;
		}
		return thisobj.Length;
	}

	public static void AddOrUpdate(this Dictionary<string, string> d, string key, string val)
	{
		d[key] = val;
	}
}
