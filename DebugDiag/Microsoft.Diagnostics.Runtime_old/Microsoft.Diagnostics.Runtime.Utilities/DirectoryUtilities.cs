using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal static class DirectoryUtilities
{
	public static void Copy(string sourceDirectory, string targetDirectory)
	{
		Copy(sourceDirectory, targetDirectory, SearchOption.AllDirectories);
	}

	public static void Copy(string sourceDirectory, string targetDirectory, SearchOption searchOptions)
	{
		if (!Directory.Exists(targetDirectory))
		{
			Directory.CreateDirectory(targetDirectory);
		}
		string[] files = Directory.GetFiles(sourceDirectory);
		foreach (string text in files)
		{
			string destinationFile = Path.Combine(targetDirectory, Path.GetFileName(text));
			FileUtilities.ForceCopy(text, destinationFile);
		}
		if (searchOptions == SearchOption.AllDirectories)
		{
			files = Directory.GetDirectories(sourceDirectory);
			foreach (string text2 in files)
			{
				string targetDirectory2 = Path.Combine(targetDirectory, Path.GetFileName(text2));
				Copy(text2, targetDirectory2, searchOptions);
			}
		}
	}

	public static int Clean(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return 0;
		}
		int num = 0;
		string[] files = Directory.GetFiles(directory);
		for (int i = 0; i < files.Length; i++)
		{
			if (!FileUtilities.ForceDelete(files[i]))
			{
				num++;
			}
		}
		files = Directory.GetDirectories(directory);
		foreach (string directory2 in files)
		{
			num += Clean(directory2);
		}
		if (num == 0)
		{
			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch
			{
				num++;
			}
		}
		else
		{
			num++;
		}
		return num;
	}

	public static bool DeleteOldest(string directoryPath, int numberToKeep)
	{
		if (!Directory.Exists(directoryPath))
		{
			return true;
		}
		string[] directories = Directory.GetDirectories(directoryPath);
		int num = directories.Length - numberToKeep;
		if (num <= 0)
		{
			return true;
		}
		Array.Sort(directories, (string x, string y) => File.GetLastWriteTimeUtc(x).CompareTo(File.GetLastWriteTimeUtc(y)));
		bool result = true;
		for (int i = 0; i < num; i++)
		{
			try
			{
				Directory.Delete(directories[i]);
			}
			catch (Exception)
			{
				result = false;
			}
		}
		return result;
	}

	public static IEnumerable<string> GetFiles(string directoryPath, string searchPattern, SearchOption searchOptions)
	{
		string[] files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		string[] array = files;
		for (int i = 0; i < array.Length; i++)
		{
			yield return array[i];
		}
		if (searchOptions != SearchOption.AllDirectories)
		{
			yield break;
		}
		string[] directories = Directory.GetDirectories(directoryPath);
		Array.Sort(directories);
		array = directories;
		foreach (string directoryPath2 in array)
		{
			foreach (string file in GetFiles(directoryPath2, searchPattern, searchOptions))
			{
				yield return file;
			}
		}
	}

	public static IEnumerable<string> GetFiles(string directoryName, string searchPattern = "*")
	{
		return GetFiles(directoryName, searchPattern, SearchOption.TopDirectoryOnly);
	}
}
