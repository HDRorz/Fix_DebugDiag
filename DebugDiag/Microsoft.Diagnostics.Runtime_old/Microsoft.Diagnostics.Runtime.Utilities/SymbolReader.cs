using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Dia2Lib;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class SymbolReader
{
	public bool CacheUnsafePdbs;

	private string _sourcePath;

	internal List<string> _parsedSourcePath;

	private static bool? s_winBuildsExist;

	private static bool? s_cpvsbuildExist;

	private static string s_symPath;

	private static Process s_currentProcess;

	internal static IntPtr s_currentProcessHandle;

	private static SymbolReaderNativeMethods.SymRegisterCallbackProc s_callback;

	internal TextWriter _log;

	private string _symbolCacheDirectory;

	private string _sourceCacheDirectory;

	public SymPath SymbolPath { get; set; }

	public string SourcePath
	{
		get
		{
			if (_sourcePath == null)
			{
				_sourcePath = Environment.GetEnvironmentVariable("_NT_SOURCE_PATH");
				if (_sourcePath == null)
				{
					_sourcePath = "";
				}
			}
			return _sourcePath;
		}
		set
		{
			_sourcePath = value;
			_parsedSourcePath = null;
		}
	}

	public string SymbolCacheDirectory
	{
		get
		{
			if (_symbolCacheDirectory == null)
			{
				_symbolCacheDirectory = SymbolPath.DefaultSymbolCache;
			}
			return _symbolCacheDirectory;
		}
		set
		{
			_symbolCacheDirectory = value;
		}
	}

	public string SourceCacheDirectory
	{
		get
		{
			if (_sourceCacheDirectory == null)
			{
				_sourceCacheDirectory = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "SrcCache");
			}
			return _sourceCacheDirectory;
		}
		set
		{
			_sourceCacheDirectory = value;
		}
	}

	public SymbolReaderFlags Flags { get; set; }

	public Func<string, bool> SecurityCheck { get; set; }

	public TextWriter Log => _log;

	internal List<string> ParsedSourcePath
	{
		get
		{
			if (_parsedSourcePath == null)
			{
				_parsedSourcePath = new List<string>();
				string[] array = SourcePath.Split(';');
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if (text.EndsWith("\\"))
					{
						text = text.Substring(0, text.Length - 1);
					}
					if (Directory.Exists(text))
					{
						_parsedSourcePath.Add(text);
					}
					else
					{
						_log.WriteLine("Path {0} in source path does not exist, skipping.", text);
					}
				}
			}
			return _parsedSourcePath;
		}
	}

	private static bool WinBuildsExist
	{
		get
		{
			if (!s_winBuildsExist.HasValue)
			{
				s_winBuildsExist = SymPath.ComputerNameExists("winbuilds");
			}
			return s_winBuildsExist.Value;
		}
	}

	private static bool CpvsbuildExist
	{
		get
		{
			if (!s_cpvsbuildExist.HasValue)
			{
				s_cpvsbuildExist = SymPath.ComputerNameExists("cpvsbuild");
			}
			return s_cpvsbuildExist.Value;
		}
	}

	public SymbolReader(TextWriter log, SymPath nt_symbol_path)
	{
		SymbolPath = nt_symbol_path;
		log.WriteLine("Created SymbolReader with SymbolPath {0}", nt_symbol_path);
		SymPath symPath = new SymPath();
		foreach (SymPathElement element in SymbolPath.Elements)
		{
			symPath.Add(element);
			if (!element.IsSymServer)
			{
				string path = Path.Combine(element.Target, "dll");
				if (Directory.Exists(path))
				{
					symPath.Add(path);
				}
				path = Path.Combine(element.Target, "exe");
				if (Directory.Exists(path))
				{
					symPath.Add(path);
				}
			}
		}
		string text = symPath.ToString();
		_log = log;
		SymbolReaderNativeMethods.SymGetOptions();
		SymbolReaderNativeMethods.SymSetOptions(SymbolReaderNativeMethods.SymOptions.SYMOPT_DEBUG | SymbolReaderNativeMethods.SymOptions.SYMOPT_EXACT_SYMBOLS | SymbolReaderNativeMethods.SymOptions.SYMOPT_LOAD_LINES | SymbolReaderNativeMethods.SymOptions.SYMOPT_UNDNAME);
		if (text != s_symPath)
		{
			StaticInit(text);
		}
	}

	static SymbolReader()
	{
		s_currentProcess = Process.GetCurrentProcess();
		s_currentProcessHandle = s_currentProcess.Handle;
		s_callback = StatusCallback;
	}

	private static void StaticInit(string newSymPathStr)
	{
		s_symPath = newSymPathStr;
		if (!SymbolReaderNativeMethods.SymInitializeW(s_currentProcessHandle, newSymPathStr, fInvadeProcess: false))
		{
			s_currentProcessHandle = IntPtr.Zero;
			throw new Win32Exception();
		}
		SymbolReaderNativeMethods.SymRegisterCallbackW64(s_currentProcessHandle, s_callback, 0uL);
	}

	public string FindSymbolFilePath(string pdbSimpleName, Guid pdbIndexGuid, int pdbIndexAge, string dllFilePath = null, string fileVersion = "")
	{
		SymbolReaderNativeMethods.SymFindFileInPathProc symFindFileInPathProc = delegate(string fileName, IntPtr context)
		{
			Guid id = Guid.Empty;
			int val = 0;
			int val2 = 0;
			if (!SymbolReaderNativeMethods.SymSrvGetFileIndexesW(fileName, ref id, ref val, ref val2, 0))
			{
				_log.WriteLine("Failed to look up PDB signature for {0}.", fileName);
				return true;
			}
			bool flag = pdbIndexGuid == id && pdbIndexAge == val;
			if (!flag)
			{
				if (pdbIndexGuid == Guid.Empty)
				{
					_log.WriteLine("No PDB Guid provided, assuming an unsafe PDB match for {0}", fileName);
					flag = true;
				}
				else
				{
					_log.WriteLine("PDB File {0} has Guid {1} age {2} != Desired Guid {3} age {4}", fileName, id, val, pdbIndexGuid, pdbIndexAge);
				}
			}
			return !flag;
		};
		StringBuilder stringBuilder = new StringBuilder(260);
		bool num = SymbolReaderNativeMethods.SymFindFileInPathW(s_currentProcessHandle, null, pdbSimpleName, ref pdbIndexGuid, pdbIndexAge, 0, 8, stringBuilder, symFindFileInPathProc, IntPtr.Zero);
		string text = null;
		if (num)
		{
			text = stringBuilder.ToString();
			goto IL_0162;
		}
		if ((Flags & SymbolReaderFlags.CacheOnly) == 0)
		{
			if (dllFilePath != null)
			{
				string text2 = Path.ChangeExtension(dllFilePath, ".pdb");
				if (!File.Exists(text2))
				{
					text2 = Path.Combine(Path.GetDirectoryName(dllFilePath), "symbols.pri\\retail\\dll\\" + Path.GetFileNameWithoutExtension(dllFilePath) + ".pdb");
				}
				if (File.Exists(text2) && !symFindFileInPathProc(text2, IntPtr.Zero) && CheckSecurity(text2))
				{
					text = text2;
					goto IL_0162;
				}
			}
			if (pdbSimpleName.IndexOf('\\') > 0 && File.Exists(pdbSimpleName) && !symFindFileInPathProc(pdbSimpleName, IntPtr.Zero) && CheckSecurity(pdbSimpleName))
			{
				text = pdbSimpleName;
				goto IL_0162;
			}
		}
		string text3 = "";
		if ((Flags & SymbolReaderFlags.CacheOnly) != 0)
		{
			text3 = " in local cache";
		}
		_log.WriteLine("Failed to find PDB {0}{1}.\r\n    GUID {2} Age {3} Version {4}", pdbSimpleName, text3, pdbIndexGuid, pdbIndexAge, fileVersion);
		return null;
		IL_0162:
		_log.WriteLine("Successfully found PDB {0}\r\n    GUID {1} Age {2} Version {3}", text, pdbIndexGuid, pdbIndexAge, fileVersion);
		return CacheFileLocally(text, pdbIndexGuid, pdbIndexAge);
	}

	public string FindSymbolFilePath(string pdbSimpleName, Guid pdbIndexGuid, int pdbIndexAge, ISymbolNotification notification)
	{
		string text = null;
		IDiaSession val = default(IDiaSession);
		foreach (SymPathElement element in SymbolPath.Elements)
		{
			if (element.IsSymServer)
			{
				pdbSimpleName = Path.GetFileName(pdbSimpleName);
				if (text == null)
				{
					text = pdbSimpleName + "\\" + pdbIndexGuid.ToString().Replace("-", "") + pdbIndexAge.ToString("x") + "\\" + pdbSimpleName;
				}
				string text2 = element.Cache;
				if (text2 == null)
				{
					text2 = SymbolPath.DefaultSymbolCache;
				}
				string fileFromServer = GetFileFromServer(element.Target, text, text2, notification);
				if (fileFromServer != null)
				{
					return fileFromServer;
				}
				continue;
			}
			string text3 = Path.Combine(element.Target, pdbSimpleName);
			_log.WriteLine("Probing file {0}", text3);
			if (File.Exists(text3))
			{
				using (new PEFile(text3))
				{
					IDiaDataSource diaSourceObject = DiaLoader.GetDiaSourceObject();
					diaSourceObject.loadDataFromPdb(text3);
					diaSourceObject.openSession(ref val);
					if (pdbIndexGuid == val.globalScope.guid)
					{
						notification.FoundSymbolOnPath(text3);
						return text3;
					}
					_log.WriteLine("Found file {0} but guid {1} != desired {2}, rejecting.", text3, val.globalScope.guid, pdbIndexGuid);
				}
			}
			notification.ProbeFailed(text3);
		}
		return null;
	}

	public string FindExecutableFilePath(string fileName, int buildTimeStamp, int sizeOfImage, ISymbolNotification notification)
	{
		string text = null;
		foreach (SymPathElement element in SymbolPath.Elements)
		{
			if (element.IsSymServer)
			{
				if (text == null)
				{
					text = fileName + "\\" + buildTimeStamp.ToString("x") + sizeOfImage.ToString("x") + "\\" + fileName;
				}
				string text2 = element.Cache;
				if (text2 == null)
				{
					text2 = SymbolPath.DefaultSymbolCache;
				}
				string fileFromServer = GetFileFromServer(element.Target, text, text2, notification);
				if (fileFromServer != null)
				{
					return fileFromServer;
				}
				continue;
			}
			string text3 = Path.Combine(element.Target, fileName);
			_log.WriteLine("Probing file {0}", text3);
			if (File.Exists(text3))
			{
				using (new PEFile(text3))
				{
					notification.FoundSymbolOnPath(text3);
					return text3;
				}
			}
			notification.ProbeFailed(text3);
		}
		return null;
	}

	internal SymbolModule OpenSymbolFile(string symbolFilePath)
	{
		return new SymbolModule(this, symbolFilePath);
	}

	private string GetPhysicalFileFromServer(string serverPath, string pdbIndexPath, string symbolCacheDir, ISymbolNotification notification)
	{
		if (string.IsNullOrEmpty(serverPath))
		{
			return null;
		}
		string text = Path.Combine(symbolCacheDir, pdbIndexPath);
		if (!File.Exists(text))
		{
			if (serverPath.StartsWith("http:"))
			{
				string text2 = serverPath + "/" + pdbIndexPath.Replace('\\', '/');
				try
				{
					HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(text2);
					obj.UserAgent = "Microsoft-Symbol-Server/6.13.0009.1140";
					Stream responseStream = obj.GetResponse().GetResponseStream();
					Path.GetDirectoryName(text);
					notification.FoundSymbolOnPath(text2);
					CopyStreamToFile(responseStream, text2, text, notification);
				}
				catch (Exception ex)
				{
					notification.ProbeFailed(text2);
					_log.WriteLine("Probe of {0} failed: {1}", text2, ex.Message);
					return null;
				}
			}
			else
			{
				string text3 = Path.Combine(serverPath, pdbIndexPath);
				if (!File.Exists(text3))
				{
					return null;
				}
				Directory.CreateDirectory(Path.GetDirectoryName(text));
				File.Copy(text3, text);
				notification.FoundSymbolInCache(text);
			}
		}
		else
		{
			notification.FoundSymbolInCache(text);
		}
		return text;
	}

	private void CopyStreamToFile(Stream fromStream, string fromUri, string fullDestPath, ISymbolNotification notification)
	{
		try
		{
			int num = 0;
			Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
			_log.WriteLine("Success Copying {0} to {1}", fromUri, fullDestPath);
			using (Stream stream = File.Create(fullDestPath))
			{
				byte[] array = new byte[8192];
				int num2 = 0;
				while (true)
				{
					int num3 = fromStream.Read(array, 0, array.Length);
					if (num3 == 0)
					{
						break;
					}
					stream.Write(array, 0, num3);
					num += num3;
					notification.DownloadProgress(num);
					_log.Write(".");
					num2++;
					if (num2 > 40)
					{
						_log.WriteLine();
						_log.Flush();
						num2 = 0;
					}
					Thread.Sleep(0);
				}
			}
			notification.DownloadComplete(fullDestPath, fullDestPath[fullDestPath.Length - 1] == '_');
		}
		finally
		{
			fromStream.Close();
			_log.WriteLine();
		}
	}

	private string GetFileFromServer(string urlForServer, string fileIndexPath, string symbolCacheDir, ISymbolNotification notification)
	{
		if (string.IsNullOrEmpty(urlForServer))
		{
			return null;
		}
		string physicalFileFromServer = GetPhysicalFileFromServer(urlForServer, fileIndexPath, symbolCacheDir, notification);
		if (physicalFileFromServer != null)
		{
			return physicalFileFromServer;
		}
		string text = Path.Combine(symbolCacheDir, fileIndexPath);
		string pdbIndexPath = fileIndexPath.Substring(0, fileIndexPath.Length - 1) + "_";
		string physicalFileFromServer2 = GetPhysicalFileFromServer(urlForServer, pdbIndexPath, symbolCacheDir, notification);
		if (physicalFileFromServer2 != null)
		{
			_log.WriteLine("Expanding {0} to {1}", physicalFileFromServer2, text);
			Command.Run("Expand " + Command.Quote(physicalFileFromServer2) + " " + Command.Quote(text));
			File.Delete(physicalFileFromServer2);
			notification.DecompressionComplete(text);
			return text;
		}
		string pdbIndexPath2 = Path.Combine(Path.GetDirectoryName(fileIndexPath), "file.ptr");
		string physicalFileFromServer3 = GetPhysicalFileFromServer(urlForServer, pdbIndexPath2, symbolCacheDir, notification);
		if (physicalFileFromServer3 != null)
		{
			string text2 = File.ReadAllText(physicalFileFromServer3).Trim();
			if (text2.StartsWith("PATH:"))
			{
				text2 = text2.Substring(5);
			}
			File.Delete(physicalFileFromServer3);
			if (!text2.StartsWith("MSG:") && File.Exists(text2))
			{
				_log.WriteLine("Copying {0} to {1}", text2, text);
				File.Copy(text2, text, overwrite: true);
				return text;
			}
			_log.WriteLine("Error resolving file.Ptr: content '{0}'", text2);
		}
		return null;
	}

	private static string GetClrDirectoryForNGenImage(string ngenImagePath, TextWriter log)
	{
		string text = "";
		Match match = Regex.Match(ngenImagePath, "^(.*)\\\\assembly\\\\NativeImages_(v(\\d+)[\\dA-Za-z.]*)_(\\d\\d)\\\\", RegexOptions.IgnoreCase);
		string value3;
		if (match.Success)
		{
			string value = match.Groups[1].Value;
			string value2 = match.Groups[2].Value;
			value3 = match.Groups[3].Value;
			text = match.Groups[4].Value;
			if (value.EndsWith(value2) && Directory.Exists(value))
			{
				return value;
			}
		}
		else
		{
			match = Regex.Match(ngenImagePath, "\\\\Microsoft\\\\CLR_v(\\d+)\\.\\d+(_(\\d\\d))?\\\\NativeImages", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				log.WriteLine("Warning: Could not deduce CLR version from path of NGEN image, skipping {0}", ngenImagePath);
				return null;
			}
			value3 = match.Groups[1].Value;
			text = match.Groups[3].Value;
		}
		if (int.Parse(value3) < 4)
		{
			log.WriteLine("Pre V4.0 native image, skipping: {0}", ngenImagePath);
			return null;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("winDir");
		if (text == "" && Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") != null)
		{
			text = "64";
		}
		if (text != "64")
		{
			text = "";
		}
		string text2 = Path.Combine(environmentVariable, "Microsoft.NET\\Framework" + text);
		string[] directories = Directory.GetDirectories(text2, "v" + value3 + ".*");
		if (directories.Length != 1)
		{
			log.WriteLine("Warning: Could not find Version {0} of the .NET Framework in {1}", value3, text2);
			return null;
		}
		return directories[0];
	}

	private bool CheckSecurity(string pdbName)
	{
		if (SecurityCheck == null)
		{
			_log.WriteLine("Found PDB {0}, however this is in an unsafe location.", pdbName);
			_log.WriteLine("If you trust this location, place this directory the symbol path to correct this.");
			return false;
		}
		if (!SecurityCheck(pdbName))
		{
			_log.WriteLine("Found PDB {0}, but failed securty check.", pdbName);
			return false;
		}
		return true;
	}

	private string CacheFileLocally(string pdbPath, Guid pdbGuid, int pdbAge)
	{
		try
		{
			string fileName = Path.GetFileName(pdbPath);
			string text = SymbolCacheDirectory;
			if (pdbGuid != Guid.Empty)
			{
				string text2 = Path.Combine(SymbolCacheDirectory, fileName);
				if (File.Exists(text2))
				{
					if (string.Compare(pdbPath, text2, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return pdbPath;
					}
					_log.WriteLine("Removing file {0} from symbol cache to make way for symsrv files.", text2);
					File.Delete(text2);
				}
				text = Path.Combine(text2, pdbGuid.ToString("N") + pdbAge);
			}
			else if (!CacheUnsafePdbs)
			{
				return pdbPath;
			}
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text3 = Path.Combine(text, fileName);
			bool flag = File.Exists(text3);
			if (!flag || File.GetLastWriteTimeUtc(text3) != File.GetLastWriteTimeUtc(pdbPath))
			{
				if (flag)
				{
					_log.WriteLine("WARNING: overwriting existing file {0}.", text3);
				}
				_log.WriteLine("Copying {0} to local cache {1}", pdbPath, text3);
				File.Copy(pdbPath, text3, overwrite: true);
			}
			return text3;
		}
		catch (Exception ex)
		{
			_log.WriteLine("Error trying to update local PDB cache {0}", ex.Message);
			return pdbPath;
		}
	}

	private unsafe static bool StatusCallback(IntPtr hProcess, SymbolReaderNativeMethods.SymCallbackActions ActionCode, ulong UserData, ulong UserContext)
	{
		bool result = false;
		if (ActionCode == SymbolReaderNativeMethods.SymCallbackActions.CBA_DEBUG_INFO || ActionCode == SymbolReaderNativeMethods.SymCallbackActions.CBA_SRCSRV_INFO)
		{
			new string((char*)UserData).Trim();
			result = true;
		}
		return result;
	}

	internal string BypassSystem32FileRedirection(string path)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("WinDir");
		if (environmentVariable != null)
		{
			string text = Path.Combine(environmentVariable, "System32");
			if (path.StartsWith(text, StringComparison.OrdinalIgnoreCase) && Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") != null)
			{
				string text2 = Path.Combine(Path.Combine(environmentVariable, "Sysnative"), path.Substring(text.Length + 1));
				if (File.Exists(text2))
				{
					path = text2;
				}
			}
		}
		return path;
	}
}
