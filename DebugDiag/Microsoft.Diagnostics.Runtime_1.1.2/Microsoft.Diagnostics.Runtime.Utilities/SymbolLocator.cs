#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime.Utilities.Pdb;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public abstract class SymbolLocator
{
	protected volatile string _symbolPath;

	protected volatile string _symbolCache;

	internal volatile HashSet<PdbEntry> _missingPdbs = new HashSet<PdbEntry>();

	internal volatile HashSet<FileEntry> _missingFiles = new HashSet<FileEntry>();

	public int Timeout { get; set; } = 60000;

	public static string MicrosoftSymbolServerPath
	{
		get
		{
			bool flag = true;
			StringBuilder stringBuilder = new StringBuilder();
			string[] microsoftSymbolServers = MicrosoftSymbolServers;
			foreach (string value in microsoftSymbolServers)
			{
				if (!flag)
				{
					stringBuilder.Append(';');
				}
				stringBuilder.Append("SRV*");
				stringBuilder.Append(value);
				flag = false;
			}
			return stringBuilder.ToString();
		}
	}

	public static string[] MicrosoftSymbolServers { get; } = new string[2] { "http://msdl.microsoft.com/download/symbols", "http://referencesource.microsoft.com/symbols" };

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

	public string SymbolCache
	{
		get
		{
			string symbolCache = _symbolCache;
			if (!string.IsNullOrEmpty(symbolCache))
			{
				return symbolCache;
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
			if (!string.IsNullOrEmpty(value))
			{
				Directory.CreateDirectory(value);
			}
			SymbolPathOrCacheChanged();
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
			_symbolPath = (value ?? "").Trim();
			SymbolPathOrCacheChanged();
		}
	}

	public async Task<string> FindBinaryAsync(string fileName, uint buildTimeStamp, uint imageSize, bool checkProperties = true)
	{
		return await FindBinaryAsync(fileName, (int)buildTimeStamp, (int)imageSize, checkProperties);
	}

	public abstract Task<string> FindBinaryAsync(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true);

	public async Task<string> FindBinaryAsync(ModuleInfo module, bool checkProperties = true)
	{
		return await FindBinaryAsync(module.FileName, module.TimeStamp, module.FileSize, checkProperties);
	}

	public async Task<string> FindBinaryAsync(DacInfo dac)
	{
		return await FindBinaryAsync(dac, checkProperties: false);
	}

	public async Task<string> FindPdbAsync(ModuleInfo module)
	{
		if (module == null)
		{
			throw new ArgumentNullException("module");
		}
		PdbInfo pdb = module.Pdb;
		if (pdb == null)
		{
			return null;
		}
		return await FindPdbAsync(pdb);
	}

	public async Task<string> FindPdbAsync(PdbInfo pdb)
	{
		if (pdb == null)
		{
			throw new ArgumentNullException("pdb");
		}
		return await FindPdbAsync(pdb.FileName, pdb.Guid, pdb.Revision);
	}

	public abstract Task<string> FindPdbAsync(string pdbName, Guid pdbIndexGuid, int pdbIndexAge);

	protected abstract Task CopyStreamToFileAsync(Stream input, string fullSrcPath, string fullDestPath, long size);

	public SymbolLocator()
	{
		string text = _NT_SYMBOL_PATH;
		if (string.IsNullOrEmpty(text))
		{
			text = MicrosoftSymbolServerPath;
		}
		SymbolPath = text;
	}

	public string FindBinary(string fileName, uint buildTimeStamp, uint imageSize, bool checkProperties = true)
	{
		return FindBinary(fileName, (int)buildTimeStamp, (int)imageSize, checkProperties);
	}

	public abstract string FindBinary(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true);

	public string FindBinary(ModuleInfo module, bool checkProperties = true)
	{
		return FindBinary(module.FileName, module.TimeStamp, module.FileSize, checkProperties);
	}

	public string FindBinary(DacInfo dac)
	{
		return FindBinary(dac, checkProperties: false);
	}

	public string FindPdb(ModuleInfo module)
	{
		if (module == null)
		{
			throw new ArgumentNullException("module");
		}
		PdbInfo pdb = module.Pdb;
		if (pdb == null)
		{
			return null;
		}
		return FindPdb(pdb);
	}

	public string FindPdb(PdbInfo pdb)
	{
		if (pdb == null)
		{
			throw new ArgumentNullException("pdb");
		}
		return FindPdb(pdb.FileName, pdb.Guid, pdb.Revision);
	}

	public abstract string FindPdb(string pdbName, Guid pdbIndexGuid, int pdbIndexAge);

	protected virtual bool ValidatePdb(string pdbName, Guid guid, int age)
	{
		try
		{
			PdbReader.GetPdbProperties(pdbName, out var signature, out var age2);
			return guid == signature && age == age2;
		}
		catch
		{
			return false;
		}
	}

	protected virtual bool ValidateBinary(string fullPath, int buildTimeStamp, int imageSize, bool checkProperties)
	{
		if (string.IsNullOrEmpty(fullPath))
		{
			return false;
		}
		if (File.Exists(fullPath))
		{
			if (!checkProperties)
			{
				return true;
			}
			try
			{
				using FileStream stream = File.OpenRead(fullPath);
				if (Path.GetExtension(fullPath) == ".so")
				{
					Debugger.Break();
					return true;
				}
				PEImage pEImage = new PEImage(stream, isVirtual: false);
				if (pEImage.IsValid)
				{
					if (!checkProperties || (pEImage.IndexTimeStamp == buildTimeStamp && pEImage.IndexFileSize == imageSize))
					{
						return true;
					}
					Trace("Rejected file '" + fullPath + "' because file size and time stamp did not match.");
				}
				else
				{
					Trace("Rejected file '" + fullPath + "' because it is not a valid PE image.");
				}
			}
			catch (Exception ex)
			{
				Trace("Encountered exception {0} while attempting to inspect file '{1}'.", ex.GetType().Name, fullPath);
			}
		}
		return false;
	}

	protected virtual void CopyStreamToFile(Stream input, string fullSrcPath, string fullDestPath, long size)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(fullDestPath);
			if (fileInfo.Exists && fileInfo.Length == size)
			{
				return;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
			FileStream fileStream = null;
			try
			{
				fileStream = new FileStream(fullDestPath, FileMode.OpenOrCreate);
				byte[] array = new byte[2048];
				int count;
				while ((count = input.Read(array, 0, array.Length)) > 0)
				{
					fileStream.Write(array, 0, count);
				}
			}
			finally
			{
				fileStream?.Dispose();
			}
		}
		catch (Exception ex)
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
			Trace("Encountered an error while attempting to copy '{0} to '{1}': {2}", fullSrcPath, fullDestPath, ex.Message);
		}
	}

	protected virtual void Trace(string fmt, params object[] args)
	{
		if (args != null && args.Length != 0)
		{
			fmt = string.Format(fmt, args);
		}
		System.Diagnostics.Trace.WriteLine(fmt, "Microsoft.Diagnostics.Runtime.SymbolLocator");
	}

	protected virtual void SymbolPathOrCacheChanged()
	{
		_missingPdbs.Clear();
		_missingFiles.Clear();
	}

	internal virtual void PrefetchBinary(string name, int timestamp, int imagesize)
	{
	}
}
