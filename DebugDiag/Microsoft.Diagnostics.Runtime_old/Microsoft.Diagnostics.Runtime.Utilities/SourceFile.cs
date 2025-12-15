using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Dia2Lib;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class SourceFile
{
	private static bool s_sourceServerCommandInProgress;

	private SymbolModule _symbolModule;

	private uint _hashType;

	private byte[] _hash;

	private bool _getSourceCalled;

	private bool _checkSumMatches;

	public string BuildTimeFilePath { get; private set; }

	public bool HasChecksum => _hash != null;

	public bool CheckSumMatches => _checkSumMatches;

	public string GetSourceFile(bool requireChecksumMatch = false)
	{
		_checkSumMatches = false;
		_getSourceCalled = true;
		TextWriter log = _symbolModule._reader._log;
		string text = null;
		if (File.Exists(BuildTimeFilePath))
		{
			text = BuildTimeFilePath;
			_checkSumMatches = ChecksumMatches(BuildTimeFilePath);
			if (_checkSumMatches)
			{
				log.WriteLine("Found in build location.");
				return BuildTimeFilePath;
			}
		}
		string sourceFromSrcServer = GetSourceFromSrcServer();
		if (sourceFromSrcServer != null)
		{
			log.WriteLine("Got source from source server.");
			_checkSumMatches = true;
			return sourceFromSrcServer;
		}
		log.WriteLine("Not present at {0} or on source server, looking on NT_SOURCE_PATH");
		List<string> parsedSourcePath = _symbolModule._reader.ParsedSourcePath;
		log.WriteLine("_NT_SOURCE_PATH={0}", _symbolModule._reader.SourcePath);
		if (_symbolModule.ExePath != null)
		{
			string directoryName = Path.GetDirectoryName(_symbolModule.ExePath);
			if (Directory.Exists(directoryName))
			{
				for (int i = 0; i < 3; i++)
				{
					parsedSourcePath.Insert(0, directoryName);
					log.WriteLine("Adding Exe path {0}", directoryName);
					directoryName = Path.GetDirectoryName(directoryName);
					if (directoryName == null)
					{
						break;
					}
				}
			}
		}
		int startIndex = 0;
		while (true)
		{
			int num = BuildTimeFilePath.IndexOf('\\', startIndex);
			if (num < 0)
			{
				break;
			}
			startIndex = num + 1;
			string text2 = BuildTimeFilePath.Substring(num);
			foreach (string item in parsedSourcePath)
			{
				string text3 = item + text2;
				log.WriteLine("Probing {0}", text3);
				if (File.Exists(text3))
				{
					if (text != null)
					{
						text = text3;
					}
					_checkSumMatches = ChecksumMatches(text3);
					if (_checkSumMatches)
					{
						log.WriteLine("Success {0}", text3);
						return text3;
					}
					log.WriteLine("Found file {0} but checksum mismatches", text3);
				}
			}
		}
		if (!requireChecksumMatch && text != null)
		{
			log.WriteLine("[Warning: Checksum mismatch for {0}]", text);
			return text;
		}
		log.WriteLine("[Could not find source for {0}]", BuildTimeFilePath);
		return null;
	}

	private string GetSourceFromSrcServer()
	{
		string ret = null;
		lock (this)
		{
			if (s_sourceServerCommandInProgress)
			{
				while (s_sourceServerCommandInProgress)
				{
					Thread.Sleep(100);
				}
			}
			s_sourceServerCommandInProgress = true;
			new Thread((ThreadStart)delegate
			{
				ret = GetSourceFromSrcServer(BuildTimeFilePath);
				s_sourceServerCommandInProgress = false;
			}).Start();
			while (s_sourceServerCommandInProgress)
			{
				Thread.Sleep(100);
			}
		}
		return ret;
	}

	private bool ChecksumMatches(string filePath)
	{
		if (!HasChecksum)
		{
			return true;
		}
		byte[] array = ComputeHash(filePath);
		if (array.Length != _hash.Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != _hash[i])
			{
				return false;
			}
		}
		return true;
	}

	private unsafe string GetSourceFromSrcServer(string buildTimeSourcePath)
	{
		ulong num = 65536uL;
		StringBuilder stringBuilder = new StringBuilder(260);
		SymbolReader reader = _symbolModule._reader;
		reader._log.WriteLine("[Searching source server for {0}]", buildTimeSourcePath);
		SymbolReaderNativeMethods.SymLoadModuleExW(SymbolReader.s_currentProcessHandle, IntPtr.Zero, _symbolModule.SourceServerPdbPath, null, num, 0u, null, 0u);
		string environmentVariable = Environment.GetEnvironmentVariable("PATH");
		string text = environmentVariable;
		string environmentVariable2 = Environment.GetEnvironmentVariable("ProgramFiles (x86)");
		if (environmentVariable2 == null)
		{
			environmentVariable2 = Environment.GetEnvironmentVariable("ProgramFiles");
		}
		if (environmentVariable2 != null)
		{
			string text2 = Path.Combine(environmentVariable2, "Microsoft Visual Studio 11.0\\Common7\\IDE");
			if (!Directory.Exists(text2))
			{
				text2 = Path.Combine(environmentVariable2, "Microsoft Visual Studio 10.0\\Common7\\IDE");
			}
			if (File.Exists(Path.Combine(text2, "tf.exe")))
			{
				reader._log.WriteLine("Adding {0} to path", text);
				text = text2 + ";" + text;
			}
			else
			{
				string text3 = "\\\\clrmain\\tools\\StandaAloneTF";
				if (SymPath.ComputerNameExists("clrmain") && Directory.Exists(text3))
				{
					reader._log.WriteLine("Adding {0} to path", text3);
					text = text2 + ";" + text3;
				}
				else
				{
					reader._log.WriteLine("Warning, could not find VS installation for TF.exe, fetching Devdiv sources may fail.");
					reader._log.WriteLine("Put TF.exe on your path to fix.");
				}
			}
		}
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);
		reader._log.WriteLine("Adding {0} to the path", directoryName);
		text = directoryName + ";" + text;
		Environment.SetEnvironmentVariable("PATH", text);
		SymbolReaderNativeMethods.SymSetHomeDirectoryW(SymbolReader.s_currentProcessHandle, reader.SourceCacheDirectory);
		bool flag = SymbolReaderNativeMethods.SymGetSourceFileW(SymbolReader.s_currentProcessHandle, num, IntPtr.Zero, buildTimeSourcePath, stringBuilder, stringBuilder.Capacity);
		reader._log.WriteLine("Called SymGetSourceFileW ret = {0}", flag);
		SymbolReaderNativeMethods.SymUnloadModule64(SymbolReader.s_currentProcessHandle, num);
		Environment.SetEnvironmentVariable("PATH", environmentVariable);
		if (!flag)
		{
			reader._log.WriteLine("Source Server for {0} failed", buildTimeSourcePath);
			return null;
		}
		string text4 = stringBuilder.ToString();
		reader._log.WriteLine("Source Server downloaded {0}", text4);
		return text4;
	}

	internal unsafe SourceFile(SymbolModule module, IDiaSourceFile sourceFile)
	{
		_symbolModule = module;
		BuildTimeFilePath = sourceFile.fileName;
		_hashType = sourceFile.checksumType;
		if (_hashType != 1 && _hashType != 0)
		{
			_hashType = 0u;
		}
		if (_hashType != 0)
		{
			_hash = new byte[16];
			fixed (byte* hash = _hash)
			{
				uint num = default(uint);
				sourceFile.get_checksum((uint)_hash.Length, ref num, ref *hash);
			}
		}
	}

	private byte[] ComputeHash(string filePath)
	{
		MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
		using FileStream inputStream = File.OpenRead(filePath);
		return mD5CryptoServiceProvider.ComputeHash(inputStream);
	}
}
