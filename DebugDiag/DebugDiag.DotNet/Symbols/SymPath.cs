using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Symbols;

/// <summary>
/// SymPath is a class that knows how to parse _NT_SYMBOL_PATH syntax.  
/// </summary>
internal class SymPath
{
	private static string s_MicrosoftSymbolServerPath;

	private List<SymPathElement> m_elements;

	private static string s_lastComputerNameLookupFailure = "";

	private static SymPath m_SymbolPath;

	/// <summary>
	/// This is the _NT_SYMBOL_PATH exposed as a SymPath type setting this sets the environment variable.
	/// If you only set _NT_SYMBOL_PATH through the SymPath class, everything stays in sync. 
	/// </summary>
	internal static SymPath SymbolPath
	{
		get
		{
			if (m_SymbolPath == null)
			{
				m_SymbolPath = new SymPath(_NT_SYMBOL_PATH);
			}
			return m_SymbolPath;
		}
		set
		{
			_NT_SYMBOL_PATH = value.ToString();
			m_SymbolPath = value;
		}
	}

	/// <summary>
	/// This allows you to set the _NT_SYMBOL_PATH as a string.  
	/// </summary>
	internal static string _NT_SYMBOL_PATH
	{
		get
		{
			string text = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
			if (text == null)
			{
				text = "";
			}
			return text;
		}
		set
		{
			m_SymbolPath = null;
			Environment.SetEnvironmentVariable("_NT_SYMBOL_PATH", value);
		}
	}

	internal static string MicrosoftSymbolServerPath
	{
		get
		{
			if (s_MicrosoftSymbolServerPath == null)
			{
				if (ComputerNameExists("symweb.corp.microsoft.com"))
				{
					s_MicrosoftSymbolServerPath = "SRV*http://symweb.corp.microsoft.com";
				}
				else
				{
					s_MicrosoftSymbolServerPath = "SRV*http://msdl.microsoft.com/download/symbols";
				}
				if (ComputerNameExists("ddrps.corp.microsoft.com"))
				{
					s_MicrosoftSymbolServerPath += ";SRV*\\\\ddrps.corp.microsoft.com\\symbols";
				}
				if (ComputerNameExists("clrmain"))
				{
					s_MicrosoftSymbolServerPath += ";SRV*\\\\clrmain\\symbols";
				}
			}
			return s_MicrosoftSymbolServerPath;
		}
	}

	internal ICollection<SymPathElement> Elements => m_elements;

	/// <summary>
	/// If you need to cache files locally, put them here.  It is defined
	/// to be the first local path of a SRV* qualification or %TEMP%\symbols
	/// if not is present.
	/// </summary>
	internal string DefaultSymbolCache
	{
		get
		{
			foreach (SymPathElement element in Elements)
			{
				if (element.IsSymServer)
				{
					if (element.Cache != null)
					{
						return element.Cache;
					}
					if (!element.IsRemote)
					{
						return element.Target;
					}
				}
			}
			string text = Environment.GetEnvironmentVariable("TEMP");
			if (text == null)
			{
				text = ".";
			}
			return Path.Combine(text, "symbols");
		}
	}

	/// <summary>
	/// This 'cleans up' a symbol path.  In particular
	/// Empty ones are replaced with good defaults (symweb or msdl)
	/// All symbol server specs have local caches (%Temp%\symbols if nothing else is specified).  
	///
	/// Note that this routine does NOT update _NT_SYMBOL_PATH.  
	/// </summary>
	internal static SymPath CleanSymbolPath()
	{
		string text = _NT_SYMBOL_PATH;
		if (text.Length == 0)
		{
			text = MicrosoftSymbolServerPath;
		}
		SymPath symPath = new SymPath(text);
		return symPath.InsureHasCache(symPath.DefaultSymbolCache).CacheFirst();
	}

	internal SymPath()
	{
		m_elements = new List<SymPathElement>();
	}

	internal SymPath(string path)
		: this()
	{
		Add(path);
	}

	internal void Add(string path)
	{
		if (path == null)
		{
			return;
		}
		path = path.Trim();
		if (path.Length != 0)
		{
			string[] array = path.Split(';');
			foreach (string strElem in array)
			{
				Add(new SymPathElement(strElem));
			}
		}
	}

	internal void Add(SymPathElement elem)
	{
		if (elem != null && !m_elements.Contains(elem))
		{
			m_elements.Add(elem);
		}
	}

	internal void Insert(string path)
	{
		string[] array = path.Split(';');
		foreach (string strElem in array)
		{
			Insert(new SymPathElement(strElem));
		}
	}

	internal void Insert(SymPathElement elem)
	{
		if (elem != null)
		{
			int num = m_elements.IndexOf(elem);
			if (num >= 0)
			{
				m_elements.RemoveAt(num);
			}
			m_elements.Insert(0, elem);
		}
	}

	/// <summary>
	/// People can use symbol servers without a local cache.  This is bad, add one if necessary. 
	/// </summary>
	internal SymPath InsureHasCache(string defaultCachePath)
	{
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in Elements)
		{
			symPath.Add(element.InsureHasCache(defaultCachePath));
		}
		return symPath;
	}

	/// <summary>
	/// Removes all references to remote paths.  This insures that network issues don't cause grief.  
	/// </summary>
	internal SymPath LocalOnly()
	{
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in Elements)
		{
			symPath.Add(element.LocalOnly());
		}
		return symPath;
	}

	internal SymPath CacheFirst()
	{
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in Elements)
		{
			if (!element.IsSymServer || !element.IsRemote)
			{
				symPath.Add(element);
			}
		}
		foreach (SymPathElement element2 in Elements)
		{
			if (element2.IsSymServer && element2.IsRemote)
			{
				symPath.Add(element2);
			}
		}
		return symPath;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (SymPathElement element in Elements)
		{
			if (!flag)
			{
				stringBuilder.Append(";");
			}
			flag = false;
			stringBuilder.Append(element.ToString());
		}
		return stringBuilder.ToString();
	}

	internal void ToXml(TextWriter log, string indent)
	{
		log.WriteLine("{0}<SymbolPaths>", indent);
		foreach (SymPathElement element in Elements)
		{
			log.WriteLine("  <SymbolPath Value=\"{0}\"/>", XmlEscape(element.ToString()));
		}
		log.WriteLine("{0}</SymbolPaths>", indent);
	}

	private string XmlEscape(object obj, bool quote = false)
	{
		string text = obj.ToString();
		StringBuilder stringBuilder = null;
		string text2 = null;
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if ((uint)c <= 38u)
			{
				if (c != '"')
				{
					if (c != '&')
					{
						continue;
					}
					text2 = "&amp;";
				}
				else
				{
					text2 = "&quot;";
				}
			}
			else if (c != '\'')
			{
				if (c != '<')
				{
					if (c != '>')
					{
						continue;
					}
					text2 = "&gt;";
				}
				else
				{
					text2 = "&lt;";
				}
			}
			else
			{
				text2 = "&apos;";
			}
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
				if (quote)
				{
					stringBuilder.Append('"');
				}
			}
			while (num < i)
			{
				stringBuilder.Append(text[num++]);
			}
			stringBuilder.Append(text2);
			num++;
		}
		if (stringBuilder != null)
		{
			while (num < text.Length)
			{
				stringBuilder.Append(text[num++]);
			}
			if (quote)
			{
				stringBuilder.Append('"');
			}
			return stringBuilder.ToString();
		}
		if (quote)
		{
			text = "\"" + text + "\"";
		}
		return text;
	}

	/// <summary>
	/// Checks to see 'computerName' exists (there is a Domain Names Service (DNS) reply to it)
	/// This routine times out quickly (after 700 msec).  
	/// </summary>
	internal static bool ComputerNameExists(string computerName, int timeoutMSec = 700)
	{
		if (computerName == s_lastComputerNameLookupFailure)
		{
			return false;
		}
		try
		{
			IPHostEntry iPHostEntry = null;
			IAsyncResult asyncResult = Dns.BeginGetHostEntry(computerName, null, null);
			if (asyncResult.AsyncWaitHandle.WaitOne(timeoutMSec))
			{
				iPHostEntry = Dns.EndGetHostEntry(asyncResult);
			}
			if (iPHostEntry != null)
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		s_lastComputerNameLookupFailure = computerName;
		return false;
	}
}
