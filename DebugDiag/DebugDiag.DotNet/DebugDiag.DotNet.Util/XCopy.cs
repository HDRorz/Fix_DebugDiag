using System;
using System.IO;

namespace DebugDiag.DotNet.Util;

public class XCopy
{
	public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool replaceWithOlder = false)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);
		if (!directoryInfo.Exists)
		{
			throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			string text = Path.Combine(destDirName, fileInfo.Name);
			if (!File.Exists(text) || replaceWithOlder || fileInfo.LastWriteTime > new FileInfo(text).LastWriteTime)
			{
				Console.WriteLine("XCopy\r\n\tFrom:\t'{0}'\r\n\tTo:\t{1}", fileInfo.FullName, text);
				fileInfo.CopyTo(text, overwrite: true);
			}
		}
		if (copySubDirs)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
				DirectoryCopy(directoryInfo2.FullName, destDirName2, copySubDirs);
			}
		}
	}
}
