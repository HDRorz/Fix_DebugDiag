using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal static class PathUtil
{
	public static string PathRelativeTo(string path, string relativeToDirectory)
	{
		string fullPath = Path.GetFullPath(path);
		string fullPath2 = Path.GetFullPath(relativeToDirectory);
		int num = -1;
		for (int i = 0; i < fullPath.Length; i++)
		{
			char c = fullPath[i];
			if (i >= fullPath2.Length)
			{
				if (c == '\\')
				{
					num = i;
				}
				break;
			}
			char c2 = fullPath2[i];
			if (c2 != c)
			{
				if (char.IsLower(c2))
				{
					c2 = (char)(c2 - 32);
				}
				if (char.IsLower(c))
				{
					c = (char)(c - 32);
				}
				if (c2 != c)
				{
					break;
				}
			}
			if (c2 == '\\')
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return path;
		}
		string text = "";
		int num2 = num;
		while (num2 < fullPath2.Length)
		{
			if (text.Length > 0)
			{
				text += "\\";
			}
			text += "..";
			if (num2 + 1 == fullPath2.Length)
			{
				break;
			}
			num2 = fullPath2.IndexOf('\\', num2 + 1);
			if (num2 < 0)
			{
				break;
			}
		}
		string path2 = fullPath.Substring(num + 1);
		return Path.Combine(text, path2);
	}
}
