using System;
using System.Text.RegularExpressions;

namespace Symbols;

/// <summary>
/// SymPathElement is a part of code:SymPath 
/// </summary>
internal class SymPathElement
{
	internal bool IsSymServer { get; private set; }

	internal string Cache { get; private set; }

	internal string Target { get; private set; }

	internal bool IsRemote
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

	internal SymPathElement(bool isSymServer, string cache, string target)
	{
		IsSymServer = isSymServer;
		Cache = cache;
		Target = target;
	}

	internal SymPathElement(string strElem)
	{
		Match match = Regex.Match(strElem, "^\\s*SRV\\*((\\s*.*?\\s*)\\*)?\\s*(.*?)\\s*$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			IsSymServer = true;
			Cache = match.Groups[2].Value;
			Target = match.Groups[3].Value;
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
