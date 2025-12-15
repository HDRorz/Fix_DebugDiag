using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class SymPath
{
	private List<SymPathElement> _elements;

	private static string s_lastComputerNameLookupFailure = "";

	private static string s_MicrosoftSymbolServerPath;

	private static SymPath s_symbolPath;

	public static SymPath SymbolPath
	{
		get
		{
			if (s_symbolPath == null)
			{
				s_symbolPath = new SymPath(_NT_SYMBOL_PATH);
			}
			return s_symbolPath;
		}
		set
		{
			_NT_SYMBOL_PATH = value.ToString();
			s_symbolPath = value;
		}
	}

	public static string _NT_SYMBOL_PATH
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
			s_symbolPath = null;
			Environment.SetEnvironmentVariable("_NT_SYMBOL_PATH", value);
		}
	}

	public static string MicrosoftSymbolServerPath
	{
		get
		{
			if (s_MicrosoftSymbolServerPath == null)
			{
				s_MicrosoftSymbolServerPath = "SRV*http://referencesource.microsoft.com/symbols;SRV*http://msdl.microsoft.com/download/symbols";
			}
			return s_MicrosoftSymbolServerPath;
		}
	}

	public ICollection<SymPathElement> Elements => _elements;

	public string DefaultSymbolCache
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

	public static SymPath CleanSymbolPath()
	{
		string text = _NT_SYMBOL_PATH;
		if (text.Length == 0)
		{
			text = MicrosoftSymbolServerPath;
		}
		SymPath symPath = new SymPath(text);
		return symPath.InsureHasCache(symPath.DefaultSymbolCache).CacheFirst();
	}

	public SymPath()
	{
		_elements = new List<SymPathElement>();
	}

	public SymPath(string path)
		: this()
	{
		Add(path);
	}

	public void Set(string path)
	{
		Clear();
		Add(path);
	}

	public void Clear()
	{
		_elements.Clear();
	}

	public void Add(string path)
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

	public void Add(SymPathElement elem)
	{
		if (elem != null && !_elements.Contains(elem))
		{
			_elements.Add(elem);
		}
	}

	public void Insert(string path)
	{
		string[] array = path.Split(';');
		foreach (string strElem in array)
		{
			Insert(new SymPathElement(strElem));
		}
	}

	public void Insert(SymPathElement elem)
	{
		if (elem != null)
		{
			int num = _elements.IndexOf(elem);
			if (num >= 0)
			{
				_elements.RemoveAt(num);
			}
			_elements.Insert(0, elem);
		}
	}

	public SymPath InsureHasCache(string defaultCachePath)
	{
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in Elements)
		{
			symPath.Add(element.InsureHasCache(defaultCachePath));
		}
		return symPath;
	}

	public SymPath LocalOnly()
	{
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in Elements)
		{
			symPath.Add(element.LocalOnly());
		}
		return symPath;
	}

	public SymPath CacheFirst()
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

	public static bool ComputerNameExists(string computerName, int timeoutMSec = 700)
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
