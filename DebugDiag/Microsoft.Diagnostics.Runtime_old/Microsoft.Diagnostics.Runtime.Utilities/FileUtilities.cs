using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal static class FileUtilities
{
	public static IEnumerable<string> ReadAllLines(string fileName)
	{
		StreamReader stream = File.OpenText(fileName);
		while (!stream.EndOfStream)
		{
			yield return stream.ReadLine();
		}
		stream.Close();
	}

	public static IEnumerable<string> ExpandWildcards(string[] fileSpecifications, SearchOption searchOpt = SearchOption.TopDirectoryOnly)
	{
		foreach (string path in fileSpecifications)
		{
			string text = Path.GetDirectoryName(path);
			if (text.Length == 0)
			{
				text = ".";
			}
			string fileName = Path.GetFileName(path);
			foreach (string file in DirectoryUtilities.GetFiles(text, fileName, searchOpt))
			{
				yield return file;
			}
		}
	}

	public static bool ForceDelete(string fileName)
	{
		if (Directory.Exists(fileName))
		{
			return DirectoryUtilities.Clean(fileName) != 0;
		}
		if (!File.Exists(fileName))
		{
			return true;
		}
		int num = 0;
		num = 0;
		string text;
		while (true)
		{
			text = fileName + "." + num + ".deleting";
			if (!File.Exists(text))
			{
				break;
			}
			num++;
		}
		File.Move(fileName, text);
		bool result = TryDelete(text);
		if (num > 0)
		{
			string searchPattern = Path.GetFileName(fileName) + ".*.deleting";
			string[] files = Directory.GetFiles(Path.GetDirectoryName(fileName), searchPattern);
			for (int i = 0; i < files.Length; i++)
			{
				TryDelete(files[i]);
			}
		}
		return result;
	}

	public static bool TryDelete(string fileName)
	{
		bool result = false;
		try
		{
			FileAttributes attributes = File.GetAttributes(fileName);
			if ((attributes & FileAttributes.ReadOnly) != 0)
			{
				attributes &= ~FileAttributes.ReadOnly;
				File.SetAttributes(fileName, attributes);
			}
			File.Delete(fileName);
			result = true;
		}
		catch (Exception)
		{
		}
		return result;
	}

	public static void ForceCopy(string sourceFile, string destinationFile)
	{
		ForceDelete(destinationFile);
		File.Copy(sourceFile, destinationFile);
	}

	public static void ForceMove(string sourceFile, string destinationFile)
	{
		ForceDelete(destinationFile);
		File.Move(sourceFile, destinationFile);
	}

	public static bool Equals(string fileName1, string fileName2)
	{
		byte[] array = new byte[8192];
		byte[] array2 = new byte[8192];
		using (FileStream fileStream = File.Open(fileName1, FileMode.Open, FileAccess.Read))
		{
			using FileStream fileStream2 = File.Open(fileName2, FileMode.Open, FileAccess.Read);
			int num = fileStream.Read(array, 0, array.Length);
			int num2 = fileStream2.Read(array2, 0, array2.Length);
			if (num != num2)
			{
				return false;
			}
			for (int i = 0; i < num; i++)
			{
				if (array[i] != array2[i])
				{
					return false;
				}
			}
		}
		return true;
	}
}
