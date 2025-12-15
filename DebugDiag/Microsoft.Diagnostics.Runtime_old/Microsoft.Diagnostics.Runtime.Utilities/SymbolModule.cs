using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Dia2Lib;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class SymbolModule
{
	private string _managedPdbName;

	private Guid _managedPdbGuid;

	private int _managedPdbAge;

	private string _managedPdbPath;

	private bool _managedPdbPathAttempted;

	internal SymbolReader _reader;

	private IDiaSession _session;

	private IDiaEnumSymbolsByAddr _symbolsByAddr;

	private string _pdbPath;

	public string PdbPath => _pdbPath;

	public string ExePath { get; set; }

	public IDiaSession Session => _session;

	public Guid PdbGuid => _session.globalScope.guid;

	public int PdbAge => (int)_session.globalScope.age;

	public SymbolReader SymbolReader => _reader;

	internal string SourceServerPdbPath
	{
		get
		{
			if (_managedPdbName == null)
			{
				return PdbPath;
			}
			if (!_managedPdbPathAttempted)
			{
				_managedPdbPathAttempted = true;
				_managedPdbPath = _reader.FindSymbolFilePath(_managedPdbName, _managedPdbGuid, _managedPdbAge);
			}
			if (_managedPdbPath == null)
			{
				return PdbPath;
			}
			return _managedPdbPath;
		}
	}

	public string FindNameForRva(uint rva)
	{
		Thread.Sleep(0);
		if (_symbolsByAddr == null)
		{
			return "";
		}
		IDiaSymbol val = _symbolsByAddr.symbolByRVA(rva);
		if (val == null)
		{
			return "";
		}
		string text = val.name;
		if (text == null)
		{
			return "";
		}
		if (text.Length == 0)
		{
			return "";
		}
		if (text.Contains("@"))
		{
			string text2 = null;
			val.get_undecoratedNameEx(4096u, ref text2);
			if (text2 != null)
			{
				text = text2;
			}
			if (text.StartsWith("@"))
			{
				text = text.Substring(1);
			}
			if (text.StartsWith("_"))
			{
				text = text.Substring(1);
			}
			int num = text.IndexOf('@');
			if (0 < num)
			{
				text = text.Substring(0, num);
			}
		}
		return text;
	}

	public SourceLocation SourceLocationForRva(uint rva)
	{
		_reader._log.WriteLine("SourceLocationForRva: looking up RVA {0:x} ", rva);
		IDiaEnumLineNumbers val = default(IDiaEnumLineNumbers);
		_session.findLinesByRVA(rva, 0u, ref val);
		IDiaLineNumber val2 = default(IDiaLineNumber);
		uint num = default(uint);
		val.Next(1u, ref val2, ref num);
		if (num == 0)
		{
			_reader._log.WriteLine("SourceLocationForRva: No lines for RVA {0:x} ", rva);
			return null;
		}
		_ = val2.sourceFile.fileName;
		_ = val2.lineNumber;
		return new SourceLocation(val2);
	}

	public SourceLocation SourceLocationForManagedCode(uint methodMetaDataToken, int ilOffset)
	{
		_reader._log.WriteLine("SourceLocationForManagedCode: Looking up method token {0:x} ilOffset {1:x}", methodMetaDataToken, ilOffset);
		IDiaSymbol val = default(IDiaSymbol);
		_session.findSymbolByToken(methodMetaDataToken, (SymTagEnum)5, ref val);
		if (val == null)
		{
			_reader._log.WriteLine("SourceLocationForManagedCode: No symbol for token {0:x} ilOffset {1:x}", methodMetaDataToken, ilOffset);
			return null;
		}
		IDiaEnumLineNumbers val2 = default(IDiaEnumLineNumbers);
		_session.findLinesByRVA(val.relativeVirtualAddress + (uint)ilOffset, 256u, ref val2);
		IDiaLineNumber val3 = default(IDiaLineNumber);
		uint num = default(uint);
		val2.Next(1u, ref val3, ref num);
		if (num == 0)
		{
			_reader._log.WriteLine("SourceLocationForManagedCode: No lines for token {0:x} ilOffset {1:x}", methodMetaDataToken, ilOffset);
			return null;
		}
		while (val3.lineNumber == 16707566)
		{
			val2.Next(1u, ref val3, ref num);
			if (num == 0)
			{
				break;
			}
		}
		return new SourceLocation(val3);
	}

	public IEnumerable<SourceFile> AllSourceFiles()
	{
		IDiaEnumTables val = default(IDiaEnumTables);
		_session.getEnumTables(ref val);
		IDiaTable val2 = null;
		uint num = 0u;
		IDiaEnumSourceFiles val3;
		do
		{
			val.Next(1u, ref val2, ref num);
			if (num == 0)
			{
				return null;
			}
			val3 = (IDiaEnumSourceFiles)(object)((val2 is IDiaEnumSourceFiles) ? val2 : null);
		}
		while (val3 == null);
		List<SourceFile> list = new List<SourceFile>();
		IDiaSourceFile sourceFile = null;
		while (true)
		{
			val3.Next(1u, ref sourceFile, ref num);
			if (num == 0)
			{
				break;
			}
			list.Add(new SourceFile(this, sourceFile));
		}
		return list;
	}

	public SymbolModule(SymbolReader reader, string pdbFilePath)
	{
		_pdbPath = pdbFilePath;
		_reader = reader;
		IDiaDataSource diaSourceObject = DiaLoader.GetDiaSourceObject();
		diaSourceObject.loadDataFromPdb(pdbFilePath);
		diaSourceObject.openSession(ref _session);
		_session.getSymbolsByAddr(ref _symbolsByAddr);
	}

	internal void LogManagedInfo(string pdbName, Guid pdbGuid, int pdbAge)
	{
		_managedPdbName = pdbName;
		_managedPdbGuid = pdbGuid;
		_managedPdbAge = pdbAge;
	}

	internal void GetSourceServerStream()
	{
		_ = Command.Run(Command.Quote(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName), "pdbstr.exe")) + "-s:srcsrv -p:" + _pdbPath).Output;
	}
}
