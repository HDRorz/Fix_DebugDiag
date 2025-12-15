using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Dia2Lib;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class SymbolLocator
{
	public delegate void FindPdbHandler(SymbolLocator sender, FindPdbEventArgs args);

	public delegate void FindBinaryHandler(SymbolLocator sender, FindBinaryEventArgs args);

	public delegate void CopyFileHandler(SymbolLocator sender, CopyFileEventArgs args);

	public delegate void ValidateBinaryHandler(SymbolLocator sender, ValidateBinaryEventArgs args);

	public enum LogLevel
	{
		Normal,
		Diagnostic
	}

	private List<SymPathElement> _symbolElements = new List<SymPathElement>();

	private Dictionary<BinaryEntry, string> _binCache = new Dictionary<BinaryEntry, string>();

	private Dictionary<PdbEntry, string> _pdbCache = new Dictionary<PdbEntry, string>();

	private Dictionary<string, SymbolModule> _moduleCache = new Dictionary<string, SymbolModule>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, PEFile> _pefileCache = new Dictionary<string, PEFile>(StringComparer.OrdinalIgnoreCase);

	private string _symbolPath;

	private string _symbolCache;

	private static string[] s_microsoftSymbolServers = new string[2] { "http://msdl.microsoft.com/download/symbols", "http://referencesource.microsoft.com/symbols" };

	public static string _NT_SYMBOL_PATH
	{
		get
		{
			return Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH") ?? "";
		}
		set
		{
			Environment.SetEnvironmentVariable("_NT_SYMBOL_PATH", value);
		}
	}

	public static string[] MicrosoftSymbolServers => s_microsoftSymbolServers;

	public static string MicrosoftSymbolServerPath
	{
		get
		{
			bool flag = true;
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = s_microsoftSymbolServers;
			foreach (string value in array)
			{
				if (!flag)
				{
					stringBuilder.Append(';');
				}
				stringBuilder.Append("SRV*");
				stringBuilder.Append(value);
			}
			return stringBuilder.ToString();
		}
	}

	public string SymbolPath
	{
		get
		{
			return _symbolPath ?? "";
		}
		set
		{
			_symbolElements = null;
			_symbolPath = (value ?? "").Trim();
		}
	}

	private List<SymPathElement> SymbolElements
	{
		get
		{
			if (_symbolElements == null)
			{
				_symbolElements = SymPathElement.GetElements(_symbolPath);
			}
			return _symbolElements;
		}
	}

	public string SymbolCache
	{
		get
		{
			if (!string.IsNullOrEmpty(_symbolCache))
			{
				return _symbolCache;
			}
			string text = Path.GetTempPath();
			if (string.IsNullOrEmpty(text))
			{
				text = ".";
			}
			return Path.Combine(text, "symbols");
		}
		set
		{
			_symbolCache = value;
			if (!string.IsNullOrEmpty(_symbolCache))
			{
				Directory.CreateDirectory(_symbolCache);
			}
		}
	}

	public event FindBinaryHandler SearchForBinary;

	public event FindPdbHandler SearchForPdb;

	public event CopyFileHandler CopyFile;

	public event ValidateBinaryHandler ValidateBinary;

	public SymbolLocator()
	{
		string text = _NT_SYMBOL_PATH;
		if (string.IsNullOrEmpty(text))
		{
			text = MicrosoftSymbolServerPath;
		}
		SymbolPath = text;
	}

	public void ClearResultCache()
	{
		_binCache.Clear();
	}

	public string FindBinary(string fileName, uint buildTimeStamp, uint imageSize, bool checkProperties = true)
	{
		return FindBinary(fileName, (int)buildTimeStamp, (int)imageSize, checkProperties);
	}

	public string FindBinary(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true)
	{
		string text = fileName;
		fileName = Path.GetFileName(text).ToLower();
		BinaryEntry key = new BinaryEntry(fileName, buildTimeStamp, imageSize);
		if (_binCache.TryGetValue(key, out var value))
		{
			if (File.Exists(value))
			{
				return value;
			}
			_binCache.Remove(key);
		}
		FindBinaryHandler findBinaryHandler = this.SearchForBinary;
		if (findBinaryHandler != null)
		{
			FindBinaryEventArgs findBinaryEventArgs = new FindBinaryEventArgs(text, buildTimeStamp, imageSize);
			Delegate[] invocationList = findBinaryHandler.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((FindBinaryHandler)invocationList[i])(this, findBinaryEventArgs);
				if (!string.IsNullOrEmpty(findBinaryEventArgs.Result))
				{
					if (CheckPathOnDisk(findBinaryEventArgs.Result, buildTimeStamp, imageSize, checkProperties))
					{
						WriteLine("Custom handler returned path '{0}' for file {1}.", findBinaryEventArgs.Result, fileName);
						_binCache[key] = findBinaryEventArgs.Result;
						return findBinaryEventArgs.Result;
					}
					WriteLine("Search for file {0} returned rejected file {1}.", text, findBinaryEventArgs.Result);
					findBinaryEventArgs.Result = null;
				}
			}
		}
		if (CheckPathOnDisk(text, buildTimeStamp, imageSize, checkProperties))
		{
			_binCache[key] = value;
			return value;
		}
		string text2 = null;
		foreach (SymPathElement symbolElement in SymbolElements)
		{
			if (symbolElement.IsSymServer)
			{
				if (text2 == null)
				{
					text2 = GetIndexPath(fileName, buildTimeStamp, imageSize);
				}
				string text3 = TryGetFileFromServer(symbolElement.Target, text2, symbolElement.Cache ?? SymbolCache);
				if (CheckPathOnDisk(text3, buildTimeStamp, imageSize, checkProperties))
				{
					_binCache[key] = text3;
					return text3;
				}
			}
			else
			{
				string text4 = Path.Combine(symbolElement.Target, fileName);
				if (CheckPathOnDisk(text4, buildTimeStamp, imageSize, checkProperties))
				{
					_binCache[key] = text4;
					return text4;
				}
			}
		}
		return null;
	}

	private static string GetIndexPath(string fileName, int buildTimeStamp, int imageSize)
	{
		return fileName + "\\" + buildTimeStamp.ToString("x") + imageSize.ToString("x") + "\\" + fileName;
	}

	private static string GetIndexPath(string pdbSimpleName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		return pdbSimpleName + "\\" + pdbIndexGuid.ToString().Replace("-", "") + pdbIndexAge.ToString("x") + "\\" + pdbSimpleName;
	}

	public string GetCachedLocation(PdbInfo pdb)
	{
		return Path.Combine(SymbolCache, GetIndexPath(Path.GetFileName(pdb.FileName), pdb.Guid, pdb.Revision));
	}

	public string GetCachedLocation(ModuleInfo module)
	{
		string fileName = module.FileName;
		uint timeStamp = module.TimeStamp;
		uint fileSize = module.FileSize;
		return GetCachedLocation(fileName, timeStamp, fileSize);
	}

	public string GetCachedLocation(string filename, uint timestamp, uint imagesize)
	{
		return Path.Combine(SymbolCache, GetIndexPath(filename, (int)timestamp, (int)imagesize));
	}

	internal SymbolModule LoadPdb(string pdbPath)
	{
		if (string.IsNullOrEmpty(pdbPath))
		{
			return null;
		}
		if (_moduleCache.TryGetValue(pdbPath, out var value))
		{
			return value;
		}
		value = new SymbolModule(new SymbolReader(null, null), pdbPath);
		_moduleCache[pdbPath] = value;
		return value;
	}

	internal SymbolModule LoadPdb(ModuleInfo module)
	{
		PdbInfo pdb = module.Pdb;
		return LoadPdb(pdb.FileName, pdb.Guid, pdb.Revision);
	}

	internal SymbolModule LoadPdb(string pdbName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		string pdbPath = FindPdb(pdbName, pdbIndexGuid, pdbIndexAge);
		return LoadPdb(pdbPath);
	}

	public string FindPdb(ModuleInfo module)
	{
		return FindPdb(module.Pdb);
	}

	public string FindPdb(PdbInfo pdb)
	{
		return FindPdb(pdb.FileName, pdb.Guid, pdb.Revision);
	}

	public string FindPdb(string pdbName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		if (string.IsNullOrEmpty(pdbName))
		{
			return null;
		}
		string fileName = Path.GetFileName(pdbName);
		if (pdbName != fileName && PdbMatches(pdbName, pdbIndexGuid, pdbIndexAge))
		{
			return pdbName;
		}
		PdbEntry key = new PdbEntry(fileName, pdbIndexGuid, pdbIndexAge);
		string value = null;
		if (_pdbCache.TryGetValue(key, out value))
		{
			return value;
		}
		string text = null;
		foreach (SymPathElement symbolElement in SymbolElements)
		{
			if (symbolElement.IsSymServer)
			{
				if (text == null)
				{
					text = GetIndexPath(fileName, pdbIndexGuid, pdbIndexAge);
				}
				string text2 = TryGetFileFromServer(symbolElement.Target, text, symbolElement.Cache ?? SymbolCache);
				if (text2 != null)
				{
					WriteLine("Found pdb {0} from server '{1}' on path '{2}'.  Copied to '{3}'.", fileName, symbolElement.Target, text, text2);
					_pdbCache[key] = text2;
					return text2;
				}
				WriteLine("No matching pdb found on server '{0}' on path '{1}'.", symbolElement.Target, text);
			}
			else
			{
				string text3 = Path.Combine(symbolElement.Target, fileName);
				if (PdbMatches(text3, pdbIndexGuid, pdbIndexAge))
				{
					_pdbCache[key] = text3;
					return text3;
				}
			}
		}
		return null;
	}

	public static bool PdbMatches(string filePath, Guid pdbIndexGuid, int revision)
	{
		if (File.Exists(filePath))
		{
			IDiaDataSource val = null;
			IDiaSession val2 = null;
			try
			{
				val = DiaLoader.GetDiaSourceObject();
				val.loadDataFromPdb(filePath);
				val.openSession(ref val2);
				if (pdbIndexGuid == val2.globalScope.guid && revision == (int)val2.globalScope.age)
				{
					return true;
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				if (val != null)
				{
					Marshal.ReleaseComObject(val);
				}
				if (val2 != null)
				{
					Marshal.ReleaseComObject(val2);
				}
			}
		}
		return false;
	}

	private string TryGetFileFromServer(string urlForServer, string fileIndexPath, string cache)
	{
		if (string.IsNullOrEmpty(urlForServer))
		{
			return null;
		}
		string physicalFileFromServer = GetPhysicalFileFromServer(urlForServer, fileIndexPath, cache);
		if (physicalFileFromServer != null)
		{
			return physicalFileFromServer;
		}
		string text = Path.Combine(cache, fileIndexPath);
		string pdbIndexPath = fileIndexPath.Substring(0, fileIndexPath.Length - 1) + "_";
		string physicalFileFromServer2 = GetPhysicalFileFromServer(urlForServer, pdbIndexPath, cache);
		if (physicalFileFromServer2 != null)
		{
			try
			{
				Command.Run("Expand " + Command.Quote(physicalFileFromServer2) + " " + Command.Quote(text));
				return text;
			}
			catch (Exception ex)
			{
				WriteLine("Exception encountered while expanding file '{0}': {1}", physicalFileFromServer2, ex.Message);
			}
			finally
			{
				if (File.Exists(physicalFileFromServer2))
				{
					File.Delete(physicalFileFromServer2);
				}
			}
		}
		string text2 = Path.Combine(Path.GetDirectoryName(fileIndexPath), "file.ptr");
		string text3 = GetPhysicalFileFromServer(urlForServer, text2, cache, returnContents: true).Trim();
		if (text3.StartsWith("PATH:"))
		{
			text3 = text3.Substring(5);
		}
		if (!text3.StartsWith("MSG:") && File.Exists(text3))
		{
			using (FileStream fileStream = File.OpenRead(text3))
			{
				CopyStreamToFile(fileStream, text3, text, fileStream.Length);
				return text;
			}
		}
		WriteLine("Error resolving file.ptr: content '{0}' from '{1}.", text3, text2);
		return null;
	}

	private string GetPhysicalFileFromServer(string serverPath, string pdbIndexPath, string symbolCacheDir, bool returnContents = false)
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
					WebResponse response = obj.GetResponse();
					using Stream stream = response.GetResponseStream();
					if (returnContents)
					{
						return new StreamReader(stream).ReadToEnd();
					}
					CopyStreamToFile(stream, text2, text, response.ContentLength);
				}
				catch (Exception ex)
				{
					WriteLine(LogLevel.Diagnostic, "Probe of {0} failed: {1}", text2, ex.Message);
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
				if (returnContents)
				{
					try
					{
						return File.ReadAllText(text3);
					}
					catch
					{
						return "";
					}
				}
				using FileStream fileStream = File.OpenRead(text3);
				CopyStreamToFile(fileStream, text3, text, fileStream.Length);
			}
		}
		else
		{
			WriteLine("Found file {0} in cache.", text);
		}
		return text;
	}

	private bool CopyStreamToFile(Stream stream, string fullSrcPath, string fullDestPath, long size)
	{
		CopyFileHandler copyFileHandler = this.CopyFile;
		if (copyFileHandler != null)
		{
			CopyFileEventArgs copyFileEventArgs = new CopyFileEventArgs(fullSrcPath, fullDestPath, stream, size);
			Delegate[] invocationList = copyFileHandler.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((CopyFileHandler)invocationList[i])(this, copyFileEventArgs);
				if (copyFileEventArgs.IsCancelled)
				{
					return false;
				}
				if (copyFileEventArgs.IsComplete)
				{
					return File.Exists(fullDestPath);
				}
			}
		}
		try
		{
			FileInfo fileInfo = new FileInfo(fullDestPath);
			if (fileInfo.Exists && fileInfo.Length == size)
			{
				return true;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
			FileStream fileStream = null;
			try
			{
				fileStream = new FileStream(fullDestPath, FileMode.OpenOrCreate);
				byte[] array = new byte[2048];
				int count;
				while ((count = stream.Read(array, 0, array.Length)) > 0)
				{
					fileStream.Write(array, 0, count);
				}
			}
			catch (IOException)
			{
				return false;
			}
			finally
			{
				fileStream?.Dispose();
			}
			return true;
		}
		catch (Exception ex2)
		{
			SafeDeleteFile(fullDestPath);
			WriteLine("Encountered an error while attempting to copy '{0} to '{1}': {2}", fullSrcPath, fullDestPath, ex2.Message);
			return false;
		}
	}

	private static void SafeDeleteFile(string fullDestPath)
	{
		try
		{
			if (File.Exists(fullDestPath))
			{
				File.Delete(fullDestPath);
			}
		}
		catch
		{
		}
	}

	public string DownloadBinary(ModuleInfo module, bool checkProperties = true)
	{
		return DownloadBinary(module.FileName, module.TimeStamp, module.FileSize, checkProperties);
	}

	public string DownloadBinary(DacInfo dac)
	{
		return DownloadBinary(dac, checkProperties: false);
	}

	public string DownloadBinary(string fileName, uint buildTimeStamp, uint imageSize, bool checkProperties = true)
	{
		return FindBinary(fileName, (int)buildTimeStamp, (int)imageSize, checkProperties);
	}

	public PEFile LoadBinary(string fileName, uint buildTimeStamp, uint imageSize, bool checkProperties = true)
	{
		string fileName2 = FindBinary(fileName, buildTimeStamp, imageSize, checkProperties);
		return LoadBinary(fileName2);
	}

	public PEFile LoadBinary(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return null;
		}
		if (_pefileCache.TryGetValue(fileName, out var value))
		{
			return value;
		}
		try
		{
			value = new PEFile(fileName);
			_pefileCache[fileName] = value;
		}
		catch
		{
			WriteLine("Failed to load PEFile '{0}'.", fileName);
		}
		return value;
	}

	private bool CheckPathOnDisk(string fullPath, int buildTimeStamp, int imageSize, bool checkProperties)
	{
		if (string.IsNullOrEmpty(fullPath))
		{
			return false;
		}
		if (File.Exists(fullPath))
		{
			ValidateBinaryHandler validateBinaryHandler = this.ValidateBinary;
			if (validateBinaryHandler != null)
			{
				ValidateBinaryEventArgs validateBinaryEventArgs = new ValidateBinaryEventArgs(fullPath, buildTimeStamp, imageSize, checkProperties);
				Delegate[] invocationList = validateBinaryHandler.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					((ValidateBinaryHandler)invocationList[i])(this, validateBinaryEventArgs);
					if (validateBinaryEventArgs.Rejected)
					{
						return false;
					}
					if (validateBinaryEventArgs.Accepted)
					{
						return true;
					}
				}
			}
			if (!checkProperties)
			{
				WriteLine("Found '{0}' for file {1}.", fullPath, Path.GetFileName(fullPath));
				return true;
			}
			try
			{
				using PEFile pEFile = new PEFile(fullPath);
				PEHeader header = pEFile.Header;
				if (!checkProperties || (header.TimeDateStampSec == buildTimeStamp && header.SizeOfImage == imageSize))
				{
					WriteLine("Found '{0}' for file {1}.", fullPath, Path.GetFileName(fullPath));
					return true;
				}
				WriteLine("Rejected file '{0}' because file size and time stamp did not match.", fullPath);
			}
			catch (Exception ex)
			{
				WriteLine("Encountered exception {0} while attempting to inspect file '{1}'.", ex.GetType().Name, fullPath);
			}
		}
		return false;
	}

	private void WriteLine(LogLevel level, string fmt, params object[] args)
	{
	}

	private void WriteLine(string fmt, params object[] args)
	{
		WriteLine(LogLevel.Normal, fmt, args);
	}
}
