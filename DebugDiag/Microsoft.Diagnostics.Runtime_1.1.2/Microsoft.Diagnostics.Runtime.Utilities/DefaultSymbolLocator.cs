using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class DefaultSymbolLocator : SymbolLocator
{
	private static readonly Dictionary<PdbEntry, Task<string>> s_pdbs = new Dictionary<PdbEntry, Task<string>>();

	private static readonly Dictionary<FileEntry, Task<string>> s_files = new Dictionary<FileEntry, Task<string>>();

	private static readonly Dictionary<string, Task> s_copy = new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

	public override async Task<string> FindBinaryAsync(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentNullException("fileName");
		}
		string fileName2 = Path.GetFileName(fileName);
		FileEntry fileEntry = new FileEntry(fileName2, buildTimeStamp, imageSize);
		HashSet<FileEntry> missingFiles = _missingFiles;
		Task<string> task = null;
		lock (s_files)
		{
			if (IsMissing(missingFiles, fileEntry))
			{
				return null;
			}
			if (!s_files.TryGetValue(fileEntry, out task))
			{
				Task<string> task3 = (s_files[fileEntry] = DownloadFileWorker(fileName, fileName2, buildTimeStamp, imageSize, checkProperties));
				task = task3;
			}
		}
		string obj = await task;
		if (obj == null)
		{
			ClearFailedTask(s_files, task, missingFiles, fileEntry);
		}
		return obj;
	}

	public override async Task<string> FindPdbAsync(string pdbName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		if (string.IsNullOrWhiteSpace(pdbName))
		{
			throw new ArgumentNullException("pdbName");
		}
		string fileName = Path.GetFileName(pdbName);
		PdbEntry pdbEntry = new PdbEntry(fileName, pdbIndexGuid, pdbIndexAge);
		HashSet<PdbEntry> missingPdbs = _missingPdbs;
		Task<string> task = null;
		lock (s_pdbs)
		{
			if (IsMissing(missingPdbs, pdbEntry))
			{
				return null;
			}
			if (!s_pdbs.TryGetValue(pdbEntry, out task))
			{
				Task<string> task3 = (s_pdbs[pdbEntry] = DownloadPdbWorker(pdbName, fileName, pdbIndexGuid, pdbIndexAge));
				task = task3;
			}
		}
		string obj = await task;
		if (obj == null)
		{
			ClearFailedTask(s_pdbs, task, missingPdbs, pdbEntry);
		}
		return obj;
	}

	private static void ClearFailedTask<T>(Dictionary<T, Task<string>> tasks, Task<string> task, HashSet<T> missingFiles, T fileEntry)
	{
		lock (tasks)
		{
			if (tasks.TryGetValue(fileEntry, out var value) && value == task)
			{
				tasks.Remove(fileEntry);
			}
			lock (missingFiles)
			{
				missingFiles.Add(fileEntry);
			}
		}
	}

	private async Task<string> DownloadPdbWorker(string pdbFullPath, string pdbSimpleName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		string indexPath = GetIndexPath(pdbSimpleName, pdbIndexGuid, pdbIndexAge);
		string fullDestPath = Path.Combine(base.SymbolCache, indexPath);
		Func<string, bool> func = (string file) => ValidatePdb(file, pdbIndexGuid, pdbIndexAge);
		string text = CheckLocalPaths(pdbFullPath, pdbSimpleName, fullDestPath, func);
		if (text != null)
		{
			return text;
		}
		return await SearchSymbolServerForFile(pdbSimpleName, indexPath, func);
	}

	private async Task<string> DownloadFileWorker(string fileFullPath, string fileSimpleName, int buildTimeStamp, int imageSize, bool checkProperties)
	{
		string indexPath = GetIndexPath(fileSimpleName, buildTimeStamp, imageSize);
		string fullDestPath = Path.Combine(base.SymbolCache, indexPath);
		Func<string, bool> func = (string file) => ValidateBinary(file, buildTimeStamp, imageSize, checkProperties);
		string text = CheckLocalPaths(fileFullPath, fileSimpleName, fullDestPath, func);
		if (text != null)
		{
			Trace("Found '{0}' locally on path '{1}'.", fileSimpleName, text);
			return text;
		}
		return await SearchSymbolServerForFile(fileSimpleName, indexPath, func);
	}

	private string CheckLocalPaths(string fullName, string simpleName, string fullDestPath, Func<string, bool> matches)
	{
		if (fullName != simpleName && matches(fullName))
		{
			return fullName;
		}
		if (File.Exists(fullDestPath))
		{
			if (matches(fullDestPath))
			{
				return fullDestPath;
			}
			File.Delete(fullDestPath);
		}
		return null;
	}

	private async Task<string> SearchSymbolServerForFile(string fileSimpleName, string fileIndexPath, Func<string, bool> match)
	{
		List<Task<string>> list = new List<Task<string>>();
		foreach (SymPathElement element in SymPathElement.GetElements(base.SymbolPath))
		{
			if (element.IsSymServer)
			{
				list.Add(TryGetFileFromServerAsync(element.Target, fileIndexPath, element.Cache ?? base.SymbolCache));
				continue;
			}
			string fullDestPath = Path.Combine(element.Cache ?? base.SymbolCache, fileIndexPath);
			string sourcePath = Path.Combine(element.Target, fileSimpleName);
			list.Add(CheckAndCopyRemoteFile(sourcePath, fullDestPath, match));
		}
		return await list.GetFirstNonNullResult();
	}

	private async Task<string> CheckAndCopyRemoteFile(string sourcePath, string fullDestPath, Func<string, bool> matches)
	{
		if (!matches(sourcePath))
		{
			return null;
		}
		try
		{
			using (Stream stream = File.OpenRead(sourcePath))
			{
				await CopyStreamToFileAsync(stream, sourcePath, fullDestPath, stream.Length);
			}
			return fullDestPath;
		}
		catch (Exception ex)
		{
			Trace("Error copying file '{0}' to '{1}': {2}", sourcePath, fullDestPath, ex);
		}
		return null;
	}

	private async Task<string> TryGetFileFromServerAsync(string urlForServer, string fileIndexPath, string cache)
	{
		string fullDestPath = Path.Combine(cache, fileIndexPath);
		if (string.IsNullOrWhiteSpace(urlForServer))
		{
			return null;
		}
		string text = fileIndexPath.Substring(0, fileIndexPath.Length - 1) + "_";
		string text2 = Path.Combine(cache, text);
		TryDeleteFile(text2);
		Task<string> physicalFileFromServerAsync = GetPhysicalFileFromServerAsync(urlForServer, text, text2);
		Task<string> rawFileDownload = GetPhysicalFileFromServerAsync(urlForServer, fileIndexPath, fullDestPath);
		string filePtrSigPath = Path.Combine(Path.GetDirectoryName(fileIndexPath), "file.ptr");
		Task<string> filePtrDownload = GetPhysicalFileFromServerAsync(urlForServer, filePtrSigPath, fullDestPath, returnContents: true);
		string text3 = await physicalFileFromServerAsync;
		if (text3 != null)
		{
			try
			{
				Command.Run("Expand " + Command.Quote(text3) + " " + Command.Quote(fullDestPath));
				Trace("Found '" + Path.GetFileName(fileIndexPath) + "' on server '" + urlForServer + "'.  Copied to '" + fullDestPath + "'.");
				return fullDestPath;
			}
			catch (Exception ex)
			{
				Trace("Exception encountered while expanding file '{0}': {1}", text3, ex.Message);
			}
			finally
			{
				if (File.Exists(text3))
				{
					File.Delete(text3);
				}
			}
		}
		text3 = await rawFileDownload;
		if (text3 != null)
		{
			Trace("Found '" + Path.GetFileName(fileIndexPath) + "' on server '" + urlForServer + "'.  Copied to '" + text3 + "'.");
			return text3;
		}
		string filePtrData = ((await filePtrDownload) ?? "").Trim();
		if (filePtrData.StartsWith("PATH:"))
		{
			filePtrData = filePtrData.Substring(5);
		}
		if (!filePtrData.StartsWith("MSG:") && File.Exists(filePtrData))
		{
			try
			{
				using (FileStream input = File.OpenRead(filePtrData))
				{
					await CopyStreamToFileAsync(input, filePtrSigPath, fullDestPath, input.Length);
				}
				Trace("Found '" + Path.GetFileName(fileIndexPath) + "' on server '" + urlForServer + "'.  Copied to '" + fullDestPath + "'.");
				return fullDestPath;
			}
			catch (Exception)
			{
				Trace("Error copying from file.ptr: content '{0}' from '{1}' to '{2}'.", filePtrData, filePtrSigPath, fullDestPath);
			}
		}
		else if (!string.IsNullOrWhiteSpace(filePtrData))
		{
			Trace("Error resolving file.ptr: content '{0}' from '{1}'.", filePtrData, filePtrSigPath);
		}
		Trace("No file matching '" + Path.GetFileName(fileIndexPath) + "' found on server '" + urlForServer + "'.");
		return null;
	}

	private void TryDeleteFile(string file)
	{
		if (File.Exists(file))
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}
		}
	}

	private async Task<string> GetPhysicalFileFromServerAsync(string serverPath, string fileIndexPath, string fullDestPath, bool returnContents = false)
	{
		if (string.IsNullOrEmpty(serverPath))
		{
			return null;
		}
		if (File.Exists(fullDestPath))
		{
			if (returnContents)
			{
				return File.ReadAllText(fullDestPath);
			}
			return fullDestPath;
		}
		if (IsHttp(serverPath))
		{
			string fullUri = serverPath + "/" + fileIndexPath.Replace('\\', '/');
			try
			{
				HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(fullUri);
				obj.UserAgent = "Microsoft-Symbol-Server/6.13.0009.1140";
				obj.Timeout = base.Timeout;
				WebResponse webResponse = await obj.GetResponseAsync();
				using Stream fromStream = webResponse.GetResponseStream();
				if (returnContents)
				{
					return await new StreamReader(fromStream).ReadToEndAsync();
				}
				Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
				await CopyStreamToFileAsync(fromStream, fullUri, fullDestPath, webResponse.ContentLength);
				Trace("Found '{0}' at '{1}'.  Copied to '{2}'.", Path.GetFileName(fileIndexPath), fullUri, fullDestPath);
				return fullDestPath;
			}
			catch (WebException)
			{
				return null;
			}
			catch (Exception ex2)
			{
				Trace("Probe of {0} failed: {1}", fullUri, ex2.Message);
				return null;
			}
		}
		string text = Path.Combine(serverPath, fileIndexPath);
		if (!File.Exists(text))
		{
			return null;
		}
		if (returnContents)
		{
			try
			{
				return File.ReadAllText(text);
			}
			catch
			{
				return "";
			}
		}
		using (FileStream fs = File.OpenRead(text))
		{
			await CopyStreamToFileAsync(fs, text, fullDestPath, fs.Length);
		}
		return fullDestPath;
	}

	private static bool IsHttp(string server)
	{
		if (!server.StartsWith("http:", StringComparison.CurrentCultureIgnoreCase))
		{
			return server.StartsWith("https:", StringComparison.CurrentCultureIgnoreCase);
		}
		return true;
	}

	protected override void SymbolPathOrCacheChanged()
	{
		_missingFiles = new HashSet<FileEntry>();
		_missingPdbs = new HashSet<PdbEntry>();
	}

	private static bool IsMissing<T>(HashSet<T> entries, T entry)
	{
		lock (entries)
		{
			return entries.Contains(entry);
		}
	}

	protected override void CopyStreamToFile(Stream stream, string fullSrcPath, string fullDestPath, long size)
	{
		Task.Run(async delegate
		{
			await CopyStreamToFileAsync(stream, fullSrcPath, fullDestPath, size);
		}).Wait();
	}

	protected override async Task CopyStreamToFileAsync(Stream input, string fullSrcPath, string fullDestPath, long size)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));
		FileStream output = null;
		try
		{
			Task value;
			lock (s_copy)
			{
				if (!s_copy.TryGetValue(fullDestPath, out value))
				{
					if (File.Exists(fullDestPath))
					{
						return;
					}
					try
					{
						Trace("Copying '{0}' from '{1}' to '{2}'.", Path.GetFileName(fullDestPath), fullSrcPath, fullDestPath);
						output = new FileStream(fullDestPath, FileMode.CreateNew);
						value = (s_copy[fullDestPath] = input.CopyToAsync(output));
					}
					catch (Exception ex)
					{
						Trace("Encountered an error while attempting to copy '{0} to '{1}': {2}", fullSrcPath, fullDestPath, ex.Message);
					}
				}
			}
			await value;
		}
		finally
		{
			output?.Dispose();
		}
	}

	private string GetFileEntry(FileEntry entry)
	{
		lock (s_files)
		{
			if (s_files.TryGetValue(entry, out var value))
			{
				return value.Result;
			}
		}
		return null;
	}

	private void SetFileEntry(HashSet<FileEntry> missingFiles, FileEntry entry, string value)
	{
		if (value != null)
		{
			lock (s_files)
			{
				if (!s_files.ContainsKey(entry))
				{
					Task<string> task = new Task<string>(() => value);
					s_files[entry] = task;
					task.Start();
				}
				return;
			}
		}
		lock (missingFiles)
		{
			missingFiles.Add(entry);
		}
	}

	private string GetPdbEntry(PdbEntry entry)
	{
		lock (s_pdbs)
		{
			if (s_pdbs.TryGetValue(entry, out var value))
			{
				return value.Result;
			}
		}
		return null;
	}

	private void SetPdbEntry(HashSet<PdbEntry> missing, PdbEntry entry, string value)
	{
		if (value != null)
		{
			lock (s_pdbs)
			{
				if (!s_pdbs.ContainsKey(entry))
				{
					Task<string> task = new Task<string>(() => value);
					s_pdbs[entry] = task;
					task.Start();
				}
				return;
			}
		}
		lock (missing)
		{
			missing.Add(entry);
		}
	}

	internal override void PrefetchBinary(string name, int timestamp, int imagesize)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(name))
			{
				new Task(async delegate
				{
					await FindBinaryAsync(name, timestamp, imagesize);
				}).Start();
			}
		}
		catch (Exception)
		{
		}
	}

	public override string FindPdb(string pdbName, Guid pdbIndexGuid, int pdbIndexAge)
	{
		if (string.IsNullOrEmpty(pdbName))
		{
			return null;
		}
		string fileName = Path.GetFileName(pdbName);
		if (pdbName != fileName && ValidatePdb(pdbName, pdbIndexGuid, pdbIndexAge))
		{
			return pdbName;
		}
		PdbEntry entry = new PdbEntry(fileName, pdbIndexGuid, pdbIndexAge);
		string pdbEntry = GetPdbEntry(entry);
		if (pdbEntry != null)
		{
			return pdbEntry;
		}
		HashSet<PdbEntry> missingPdbs = _missingPdbs;
		if (IsMissing(missingPdbs, entry))
		{
			return null;
		}
		string indexPath = GetIndexPath(fileName, pdbIndexGuid, pdbIndexAge);
		foreach (SymPathElement element in SymPathElement.GetElements(base.SymbolPath))
		{
			if (element.IsSymServer)
			{
				string text = TryGetFileFromServer(element.Target, indexPath, element.Cache ?? base.SymbolCache);
				if (text != null)
				{
					Trace("Found pdb {0} from server '{1}' on path '{2}'.  Copied to '{3}'.", fileName, element.Target, indexPath, text);
					SetPdbEntry(missingPdbs, entry, text);
					return text;
				}
				Trace("No matching pdb found on server '{0}' on path '{1}'.", element.Target, indexPath);
			}
			else
			{
				string text2 = Path.Combine(element.Target, fileName);
				if (ValidatePdb(text2, pdbIndexGuid, pdbIndexAge))
				{
					Trace("Found pdb '" + fileName + "' at '" + text2 + "'.");
					SetPdbEntry(missingPdbs, entry, text2);
					return text2;
				}
				Trace("Mismatched pdb found at '" + text2 + "'.");
			}
		}
		SetPdbEntry(missingPdbs, entry, null);
		return null;
	}

	public override string FindBinary(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true)
	{
		string text = fileName;
		fileName = Path.GetFileName(text).ToLower();
		FileEntry entry = new FileEntry(fileName, buildTimeStamp, imageSize);
		string fileEntry = GetFileEntry(entry);
		if (fileEntry != null)
		{
			return fileEntry;
		}
		HashSet<FileEntry> missingFiles = _missingFiles;
		if (IsMissing(missingFiles, entry))
		{
			return null;
		}
		if (ValidateBinary(text, buildTimeStamp, imageSize, checkProperties))
		{
			SetFileEntry(missingFiles, entry, text);
			return text;
		}
		string text2 = null;
		string text3 = base.SymbolCache;
		foreach (SymPathElement element in SymPathElement.GetElements(base.SymbolPath))
		{
			if (element.IsSymServer)
			{
				if (text2 == null)
				{
					text2 = GetIndexPath(fileName, buildTimeStamp, imageSize);
				}
				string text4 = TryGetFileFromServer(element.Target, text2, element.Cache ?? text3);
				if (text4 == null)
				{
					Trace($"Server '{element.Target}' did not have file '{Path.GetFileName(fileName)}' with timestamp={buildTimeStamp:x} and filesize={imageSize:x}.");
				}
				else if (ValidateBinary(text4, buildTimeStamp, imageSize, checkProperties))
				{
					Trace("Found '" + fileName + "' on server '" + element.Target + "'.  Copied to '" + text4 + "'.");
					SetFileEntry(missingFiles, entry, text4);
					return text4;
				}
			}
			else if (element.IsCache)
			{
				text3 = (string.IsNullOrEmpty(element.Cache) ? base.SymbolCache : element.Cache);
			}
			else
			{
				string text5 = Path.Combine(element.Target, fileName);
				if (ValidateBinary(text5, buildTimeStamp, imageSize, checkProperties))
				{
					Trace("Found '" + fileName + "' at '" + text5 + "'.");
					SetFileEntry(missingFiles, entry, text5);
					return text5;
				}
			}
		}
		SetFileEntry(missingFiles, entry, null);
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

	private string TryGetFileFromServer(string urlForServer, string fileIndexPath, string cache)
	{
		if (string.IsNullOrEmpty(urlForServer))
		{
			return null;
		}
		string text = Path.Combine(cache, fileIndexPath);
		string pdbIndexPath = fileIndexPath.Substring(0, fileIndexPath.Length - 1) + "_";
		string physicalFileFromServer = GetPhysicalFileFromServer(urlForServer, pdbIndexPath, cache);
		if (physicalFileFromServer != null)
		{
			try
			{
				Command.Run("Expand " + Command.Quote(physicalFileFromServer) + " " + Command.Quote(text));
				return text;
			}
			catch (Exception ex)
			{
				Trace("Exception encountered while expanding file '{0}': {1}", physicalFileFromServer, ex.Message);
			}
			finally
			{
				if (File.Exists(physicalFileFromServer))
				{
					File.Delete(physicalFileFromServer);
				}
			}
		}
		string physicalFileFromServer2 = GetPhysicalFileFromServer(urlForServer, fileIndexPath, cache);
		if (physicalFileFromServer2 != null)
		{
			return physicalFileFromServer2;
		}
		string text2 = Path.Combine(Path.GetDirectoryName(fileIndexPath), "file.ptr");
		string physicalFileFromServer3 = GetPhysicalFileFromServer(urlForServer, text2, cache, returnContents: true);
		if (physicalFileFromServer3 == null)
		{
			return null;
		}
		physicalFileFromServer3 = physicalFileFromServer3.Trim();
		if (physicalFileFromServer3.StartsWith("PATH:"))
		{
			physicalFileFromServer3 = physicalFileFromServer3.Substring(5);
		}
		if (!physicalFileFromServer3.StartsWith("MSG:") && File.Exists(physicalFileFromServer3))
		{
			using (FileStream fileStream = File.OpenRead(physicalFileFromServer3))
			{
				CopyStreamToFile(fileStream, physicalFileFromServer3, text, fileStream.Length);
				return text;
			}
		}
		Trace("Error resolving file.ptr: content '{0}' from '{1}.", physicalFileFromServer3, text2);
		return null;
	}

	private string GetPhysicalFileFromServer(string serverPath, string pdbIndexPath, string symbolCacheDir, bool returnContents = false)
	{
		if (string.IsNullOrEmpty(serverPath))
		{
			return null;
		}
		string text = Path.Combine(symbolCacheDir, pdbIndexPath);
		if (File.Exists(text))
		{
			return text;
		}
		if (serverPath.StartsWith("http:") || serverPath.StartsWith("https:"))
		{
			string text2 = serverPath + "/" + pdbIndexPath.Replace('\\', '/');
			try
			{
				HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(text2);
				obj.UserAgent = "Microsoft-Symbol-Server/6.13.0009.1140";
				obj.Timeout = base.Timeout;
				WebResponse response = obj.GetResponse();
				using Stream stream = response.GetResponseStream();
				if (returnContents)
				{
					return new StreamReader(stream).ReadToEnd();
				}
				CopyStreamToFile(stream, text2, text, response.ContentLength);
				return text;
			}
			catch (WebException)
			{
				return null;
			}
			catch (Exception ex2)
			{
				Trace("Probe of {0} failed: {1}", text2, ex2.Message);
				return null;
			}
		}
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
		return text;
	}
}
