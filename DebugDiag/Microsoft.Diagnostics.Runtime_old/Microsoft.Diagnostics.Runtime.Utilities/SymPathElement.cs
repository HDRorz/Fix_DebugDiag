using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class SymPathElement
{
	public bool IsSymServer { get; private set; }

	public string Cache { get; set; }

	public string Target { get; private set; }

	public bool IsRemote
	{
		get
		{
			if (Target != null)
			{
				if (Target.StartsWith("\\\\"))
				{
					return true;
				}
				if (2 <= Target.Length && Target[1] == ':')
				{
					char c = char.ToUpperInvariant(Target[0]);
					if ('T' <= c && c <= 'Z')
					{
						return true;
					}
				}
			}
			if (!IsSymServer)
			{
				return false;
			}
			if (Cache != null)
			{
				return true;
			}
			if (Target == null)
			{
				return false;
			}
			if (Target.StartsWith("http:/", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			return false;
		}
	}

	public static List<SymPathElement> GetElements(string symbolPath)
	{
		List<SymPathElement> list = new List<SymPathElement>();
		string[] array = (symbolPath ?? "").Split(';');
		list = new List<SymPathElement>(array.Length);
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new SymPathElement(text));
			}
		}
		return list;
	}

	public override string ToString()
	{
		if (IsSymServer)
		{
			string text = "SRV";
			if (Cache != null)
			{
				text = text + "*" + Cache;
			}
			if (Target != null)
			{
				text = text + "*" + Target;
			}
			return text;
		}
		return Target;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is SymPathElement symPathElement))
		{
			return false;
		}
		if (Target == symPathElement.Target && Cache == symPathElement.Cache)
		{
			return IsSymServer == symPathElement.IsSymServer;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Target.GetHashCode();
	}

	internal SymPathElement InsureHasCache(string defaultCachePath)
	{
		if (!IsSymServer)
		{
			return this;
		}
		if (Cache != null)
		{
			return this;
		}
		if (Target == defaultCachePath)
		{
			return this;
		}
		return new SymPathElement(isSymServer: true, defaultCachePath, Target);
	}

	internal SymPathElement LocalOnly()
	{
		if (!IsRemote)
		{
			return this;
		}
		if (Cache != null)
		{
			return new SymPathElement(isSymServer: true, null, Cache);
		}
		return null;
	}

	public SymPathElement(bool isSymServer, string cache, string target)
	{
		IsSymServer = isSymServer;
		Cache = cache;
		Target = target;
	}

	internal SymPathElement(string strElem)
	{
		Match match = Regex.Match(strElem, "^\\s*(SRV\\*|http:)((\\s*.*?\\s*)\\*)?\\s*(.*?)\\s*$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			IsSymServer = true;
			Cache = match.Groups[3].Value;
			if (match.Groups[1].Value.Equals("http:", StringComparison.CurrentCultureIgnoreCase))
			{
				Target = "http:" + match.Groups[4].Value;
			}
			else
			{
				Target = match.Groups[4].Value;
			}
			if (Cache.Length == 0)
			{
				Cache = null;
			}
			if (Target.Length == 0)
			{
				Target = null;
			}
		}
		else
		{
			match = Regex.Match(strElem, "^\\s*CACHE\\*(.*?)\\s*$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				IsSymServer = true;
				Cache = match.Groups[1].Value;
			}
			else
			{
				Target = strElem.Trim();
			}
		}
	}
}
