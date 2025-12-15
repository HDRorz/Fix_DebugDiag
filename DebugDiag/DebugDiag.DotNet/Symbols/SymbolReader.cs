using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using PEFile;
using Utilities;

namespace Symbols;

/// <summary>
/// A symbol reader represents something that can FIND pdbs (either on a symbol server or via a symbol path)
/// Its job is to find a full path a PDB.  Then you can use OpenSymbolFile to get a SymbolReaderModule and do more. 
/// </summary>
internal class SymbolReader : IDisposable
{
	/// <summary>
	/// Cache even the unsafe pdbs to the SymbolCacheDirectory.   TOODOO: is this a hack?
	/// </summary>
	internal bool CacheUnsafePdbs;

	private string m_SourcePath;

	internal List<string> m_parsedSourcePath;

	private static bool? m_WinBuildsExist;

	private static bool? m_CpvsbuildExist;

	internal Process m_currentProcess;

	internal IntPtr m_currentProcessHandle;

	internal SymbolReaderNativeMethods.SymRegisterCallbackProc m_callback;

	internal TextWriter m_log;

	private string m_SymbolCacheDirectory;

	private string m_SourceCacheDirectory;

	/// <summary>
	/// The symbol path used to look up PDB symbol files.   Set when the reader is initialized.  
	/// </summary>
	internal string SymbolPath { get; set; }

	/// <summary>
	/// The paths used to look up source files.  defaults to _NT_SOURCE_PATH.  
	/// </summary>
	internal string SourcePath
	{
		get
		{
			if (m_SourcePath == null)
			{
				m_SourcePath = Environment.GetEnvironmentVariable("_NT_SOURCE_PATH");
				if (m_SourcePath == null)
				{
					m_SourcePath = "";
				}
			}
			return m_SourcePath;
		}
		set
		{
			m_SourcePath = value;
			m_parsedSourcePath = null;
		}
	}

	/// <summary>
	/// Where symbols are downloaded if needed.   Derived from symbol path
	/// </summary>
	internal string SymbolCacheDirectory
	{
		get
		{
			if (m_SymbolCacheDirectory == null)
			{
				m_SymbolCacheDirectory = new SymPath(SymbolPath).DefaultSymbolCache;
			}
			return m_SymbolCacheDirectory;
		}
		set
		{
			m_SymbolCacheDirectory = value;
		}
	}

	/// <summary>
	/// The place where source is downloaded from a source server.  
	/// </summary>
	internal string SourceCacheDirectory
	{
		get
		{
			if (m_SourceCacheDirectory == null)
			{
				m_SourceCacheDirectory = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "SrcCache");
			}
			return m_SourceCacheDirectory;
		}
		set
		{
			m_SourceCacheDirectory = value;
		}
	}

	/// <summary>
	/// We call back on this when we find a PDB by probing in 'unsafe' locations (like next to the EXE or in the Built location)
	/// If this function returns true, we assume that it is OK to use the PDB.  
	/// </summary>
	internal Func<string, bool> SecurityCheck { get; set; }

	/// <summary>
	/// A place to log additional messages 
	/// </summary>
	internal TextWriter Log => m_log;

	internal bool IsDisposed => m_currentProcess == null;

	internal List<string> ParsedSourcePath
	{
		get
		{
			if (m_parsedSourcePath == null)
			{
				m_parsedSourcePath = new List<string>();
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
						m_parsedSourcePath.Add(text);
					}
					else
					{
						m_log.WriteLine("Path {0} in source path does not exist, skipping.", text);
					}
				}
			}
			return m_parsedSourcePath;
		}
	}

	private static bool WinBuildsExist
	{
		get
		{
			if (!m_WinBuildsExist.HasValue)
			{
				m_WinBuildsExist = SymPath.ComputerNameExists("winbuilds");
			}
			return m_WinBuildsExist.Value;
		}
	}

	private static bool CpvsbuildExist
	{
		get
		{
			if (!m_CpvsbuildExist.HasValue)
			{
				m_CpvsbuildExist = SymPath.ComputerNameExists("cpvsbuild");
			}
			return m_CpvsbuildExist.Value;
		}
	}

	/// <summary>
	/// Opens a new SymbolReader.   All diagnostics messages about symbol lookup go to 'log'.  
	/// </summary>
	internal SymbolReader(TextWriter log, string nt_symbol_path = null)
	{
		SymbolPath = nt_symbol_path;
		if (SymbolPath == null)
		{
			SymbolPath = SymPath._NT_SYMBOL_PATH;
		}
		log.WriteLine("Created SymbolReader with SymbolPath {0}", nt_symbol_path);
		SymPath symPath = new SymPath(SymbolPath);
		SymPath symPath2 = new SymPath();
		foreach (SymPathElement element in symPath.Elements)
		{
			symPath2.Add(element);
			if (!element.IsSymServer)
			{
				string path = Path.Combine(element.Target, "dll");
				if (Directory.Exists(path))
				{
					symPath2.Add(path);
				}
				path = Path.Combine(element.Target, "exe");
				if (Directory.Exists(path))
				{
					symPath2.Add(path);
				}
			}
		}
		string userSearchPath = symPath2.ToString();
		m_log = log;
		SymbolReaderNativeMethods.SymGetOptions();
		SymbolReaderNativeMethods.SymSetOptions(SymbolReaderNativeMethods.SymOptions.SYMOPT_DEBUG | SymbolReaderNativeMethods.SymOptions.SYMOPT_EXACT_SYMBOLS | SymbolReaderNativeMethods.SymOptions.SYMOPT_LOAD_LINES | SymbolReaderNativeMethods.SymOptions.SYMOPT_UNDNAME);
		m_currentProcess = Process.GetCurrentProcess();
		m_currentProcessHandle = m_currentProcess.Handle;
		if (!SymbolReaderNativeMethods.SymInitializeW(m_currentProcessHandle, userSearchPath, fInvadeProcess: false))
		{
			m_currentProcessHandle = IntPtr.Zero;
			throw new Win32Exception();
		}
	}

	/// <summary>
	/// Note that all SymbolReaderModules returned by 'OpenSymbolFile' become invalid after disposing of the SymbolReader.  
	/// </summary>
	public void Dispose()
	{
		if (m_currentProcessHandle != IntPtr.Zero)
		{
			SymbolReaderNativeMethods.SymCleanup(m_currentProcessHandle);
			m_currentProcessHandle = IntPtr.Zero;
			m_currentProcess.Close();
			m_currentProcess = null;
		}
	}

	~SymbolReader()
	{
	}

	internal static void Test()
	{
	}

	/// <summary>
	/// Fetches a file from the server 'serverPath' weith pdb signature path 'pdbSigPath' and places it in its
	/// correct location in 'symbolCacheDir'  Will return the path of the cached copy if it succeeds, null otherwise.  
	///
	/// You should probably be using GetFileFromServer
	/// </summary>
	/// <param name="serverPath">path to server (eg. \\symbols\symbols or http://symweb) </param>
	/// <param name="pdbIndexPath">pdb path with signature (e.g clr.pdb/1E18F3E494DC464B943EA90F23E256432/clr.pdb)</param>
	/// <param name="symbolCacheDir">path to the symbol cache where files are fetched (e.g. %TEMP%\symbols) </param>
	private string GetPhysicalFileFromServer(string serverPath, string pdbIndexPath, string symbolCacheDir)
	{
		string text = Path.Combine(symbolCacheDir, pdbIndexPath);
		if (!File.Exists(text))
		{
			if (!string.IsNullOrEmpty(serverPath) && serverPath.StartsWith("http:"))
			{
				string text2 = serverPath + "/" + pdbIndexPath.Replace('\\', '/');
				try
				{
					HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(text2);
					obj.UserAgent = "Microsoft-Symbol-Server/6.13.0009.1140";
					Stream responseStream = obj.GetResponse().GetResponseStream();
					Path.GetDirectoryName(text);
					CopyStreamToFile(responseStream, text2, text);
				}
				catch (Exception ex)
				{
					m_log.WriteLine("Probe of {0} failed: {1}", text2, ex.Message);
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
				File.Copy(text3, text);
			}
		}
		return text;
	}

	/// <summary>
	/// This just copies a stream to a file path with logging.  
	/// </summary>
	private void CopyStreamToFile(Stream fromStream, string fromUri, string fullDestPath)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
			m_log.WriteLine("Success Copying {0} to {1}", fromUri, fullDestPath);
			using Stream stream = File.Create(fullDestPath);
			byte[] array = new byte[8192];
			int num = 0;
			while (true)
			{
				int num2 = fromStream.Read(array, 0, array.Length);
				if (num2 == 0)
				{
					break;
				}
				stream.Write(array, 0, num2);
				m_log.Write(".");
				num++;
				if (num > 40)
				{
					m_log.WriteLine();
					m_log.Flush();
					num = 0;
				}
				Thread.Sleep(0);
			}
		}
		finally
		{
			fromStream.Close();
			m_log.WriteLine();
		}
	}

	/// <summary>
	/// This API looks up an executable file, by its build-timestamp and size (on a symbol server),  'fileName' should be 
	/// a simple name (no directory), and you need the buildTimeStamp and sizeOfImage that are found in the PE header.
	///
	/// Returns null if it cannot find anything.  
	/// </summary>
	internal string FindExecutableFilePath(string fileName, int buildTimeStamp, int sizeOfImage)
	{
		string text = null;
		SymPath symPath = new SymPath(SymbolPath);
		foreach (SymPathElement element in symPath.Elements)
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
					text2 = symPath.DefaultSymbolCache;
				}
				string fileFromServer = GetFileFromServer(element.Target, text, text2);
				if (fileFromServer != null)
				{
					return fileFromServer;
				}
				continue;
			}
			string text3 = Path.Combine(element.Target, fileName);
			m_log.WriteLine("Probing file {0}", text3);
			if (File.Exists(text3))
			{
				using (new global::PEFile.PEFile(text3))
				{
					return text3;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Looks up 'fileIndexPath' on the server 'urlForServer' copying the file to 'symbolCacheDir' and returning the
	/// path name there (thus it is always a local file).  Unlike  GetPhysicalFileFromServer, GetFileFromServer understands
	/// how to deal with compressed files and file.ptr (redirection).  
	/// </summary>
	/// <returns>The path to a local file or null if the file cannot be found.</returns>
	private string GetFileFromServer(string urlForServer, string fileIndexPath, string symbolCacheDir)
	{
		string physicalFileFromServer = GetPhysicalFileFromServer(urlForServer, fileIndexPath, symbolCacheDir);
		if (physicalFileFromServer != null)
		{
			return physicalFileFromServer;
		}
		string text = Path.Combine(symbolCacheDir, fileIndexPath);
		string pdbIndexPath = fileIndexPath.Substring(0, fileIndexPath.Length - 1) + "_";
		string physicalFileFromServer2 = GetPhysicalFileFromServer(urlForServer, pdbIndexPath, symbolCacheDir);
		if (physicalFileFromServer2 != null)
		{
			m_log.WriteLine("Expanding {0} to {1}", physicalFileFromServer2, text);
			Command.Run("Expand " + Command.Quote(physicalFileFromServer2) + " " + Command.Quote(text));
			File.Delete(physicalFileFromServer2);
			return text;
		}
		string pdbIndexPath2 = Path.Combine(Path.GetDirectoryName(fileIndexPath), "file.ptr");
		string physicalFileFromServer3 = GetPhysicalFileFromServer(urlForServer, pdbIndexPath2, symbolCacheDir);
		if (physicalFileFromServer3 != null)
		{
			string text2 = File.ReadAllText(physicalFileFromServer3).Trim();
			if (text2.StartsWith("PATH:"))
			{
				text2 = text2.Substring(5);
			}
			File.Delete(physicalFileFromServer3);
			if (File.Exists(text2))
			{
				m_log.WriteLine("Copying {0} to {1}", text2, text);
				File.Copy(text2, text, overwrite: true);
				return text;
			}
			m_log.WriteLine("Error resolving file.Ptr: content '{0}'", text2);
		}
		return null;
	}

	private bool CheckSecurity(string pdbName)
	{
		if (SecurityCheck == null)
		{
			m_log.WriteLine("Found PDB {0}, however this is in an unsafe location.", pdbName);
			m_log.WriteLine("If you trust this location, place this directory the symbol path to correct this.");
			return false;
		}
		if (!SecurityCheck(pdbName))
		{
			m_log.WriteLine("Found PDB {0}, but failed securty check.", pdbName);
			return false;
		}
		return true;
	}

	/// <summary>
	/// This is an optional routine.  It is already the case that if you find a PDB on a symbol server
	/// that it will be cached locally, however if you find it on a network path by NOT using a symbol
	/// server, it will be used in place.  This is annoying, and this routine makes up for this by
	/// mimicking this behavior.  Basically if pdbPath is not a local file name, it will copy it to
	/// the local symbol cache and return the local path. 
	/// </summary>
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
					m_log.WriteLine("Removing file {0} from symbol cache to make way for symsrv files.", text2);
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
					m_log.WriteLine("WARNING: overwriting existing file {0}.", text3);
				}
				m_log.WriteLine("Copying {0} to local cache {1}", pdbPath, text3);
				File.Copy(pdbPath, text3, overwrite: true);
			}
			return text3;
		}
		catch (Exception ex)
		{
			m_log.WriteLine("Error trying to update local PDB cache {0}", ex.Message);
			return pdbPath;
		}
	}

	private unsafe bool StatusCallback(IntPtr hProcess, SymbolReaderNativeMethods.SymCallbackActions ActionCode, ulong UserData, ulong UserContext)
	{
		bool result = false;
		if (ActionCode == SymbolReaderNativeMethods.SymCallbackActions.CBA_DEBUG_INFO || ActionCode == SymbolReaderNativeMethods.SymCallbackActions.CBA_SRCSRV_INFO)
		{
			string input = new string((char*)UserData).Trim();
			m_log.WriteLine(Regex.Replace(input, "\\p{C}+", string.Empty));
			result = true;
		}
		return result;
	}

	/// <summary>
	/// We may be a 32 bit app which has File system redirection turned on
	/// Morph System32 to SysNative in that case to bypass file system redirection         
	/// </summary>
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

	/// <summary>
	/// This function is useful only internally in Microsoft.  It is useful for builds that are not 
	/// published on the symbol server.   It knows about windows conventions as well as devdiv conventions
	/// </summary>
	private string FindPrivateBuildPdbSearchPath(string fileVersion, string arch)
	{
		Match match = Regex.Match(fileVersion, "(\\d+)\\.(\\d+)\\.(\\d+)\\.(\\d+).*\\((\\w+)\\.([\\d-]+)\\)");
		if (match.Success && !fileVersion.Contains("win7_"))
		{
			if (!WinBuildsExist)
			{
				m_log.WriteLine("WinBuilds for arch {0} not accessable.  No private build lookup done.", arch);
				return null;
			}
			string value = match.Groups[3].Value;
			string value2 = match.Groups[4].Value;
			string value3 = match.Groups[5].Value;
			string value4 = match.Groups[6].Value;
			string text = "fre";
			string text2 = $"\\\\winbuilds\\release\\{value3}\\{value}.{value2}.{value4}\\{arch}{text}";
			if (!Directory.Exists(text2))
			{
				m_log.WriteLine("Windows Drop Path {0} does not exist", text2);
				return null;
			}
			string text3 = Path.Combine(text2, "symbols.pri\\retail");
			if (Directory.Exists(text3))
			{
				m_log.WriteLine("Found Windows symbols {0}.", text3);
				return text3;
			}
			m_log.WriteLine("Could not pdbs at {0}.", text2);
		}
		match = Regex.Match(fileVersion, "(\\d+)\\.(\\d+)\\.(\\d+)\\.(\\d+).*built by: *(\\w+)");
		if (match.Success)
		{
			if (!CpvsbuildExist)
			{
				m_log.WriteLine("Cpvsbuild for arch {0} not accessable.  No private build lookup done.", arch);
				return null;
			}
			m_log.WriteLine("File version {0} matches devdiv schema.", fileVersion);
			string value5 = match.Groups[1].Value;
			string value6 = match.Groups[3].Value;
			string value7 = match.Groups[4].Value;
			string value8 = match.Groups[5].Value;
			string text4 = "ret";
			string text5 = $"\\\\cpvsbuild\\drops\\dev{value5}\\{value8}\\raw\\{value6}.{value7.PadLeft(2, '0')}\\binaries.{arch}{text4}";
			string text6 = Path.Combine(text5, "symbols.pri\\retail");
			if (Directory.Exists(text6))
			{
				m_log.WriteLine("Found DevDiv symbols {0}.", text6);
				return text6;
			}
			m_log.WriteLine("Could not find private build {0}.", text5);
		}
		return null;
	}

	internal string FindPdbInPrivateBuilds(string pdbSimpleName, string fileVersion, SymbolReaderNativeMethods.SymFindFileInPathProc FindSymbolFileCallBack)
	{
		if (string.IsNullOrEmpty(fileVersion))
		{
			return null;
		}
		string[] array = new string[2] { "x86", "amd64" };
		foreach (string arch in array)
		{
			string text = FindPrivateBuildPdbSearchPath(fileVersion, arch);
			if (text == null)
			{
				continue;
			}
			pdbSimpleName = Path.GetFileNameWithoutExtension(pdbSimpleName);
			string text2 = Path.Combine(text, "dll\\" + pdbSimpleName + ".pdb");
			if (!File.Exists(text2))
			{
				text2 = Path.Combine(text, "exe\\" + pdbSimpleName + ".pdb");
				if (!File.Exists(text2))
				{
					continue;
				}
			}
			if (!FindSymbolFileCallBack(text2, IntPtr.Zero))
			{
				return text2;
			}
		}
		return null;
	}
}
