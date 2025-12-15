namespace PEFile;

internal class FileVersionInfo
{
	internal string FileVersion { get; private set; }

	internal unsafe FileVersionInfo(byte* data, int dataLen)
	{
		FileVersion = "";
		if (dataLen <= 92)
		{
			return;
		}
		string text = new string((char*)(data + 92), 0, (dataLen - 92) / 2);
		string text2 = "FileVersion";
		int num = text.IndexOf(text2);
		if (num < 0)
		{
			return;
		}
		int num2 = num + text2.Length;
		do
		{
			num2++;
			if (num2 >= text.Length)
			{
				return;
			}
		}
		while (text[num2] == '\0');
		int num3 = text.IndexOf('\0', num2);
		if (num3 >= 0)
		{
			FileVersion = text.Substring(num2, num3 - num2);
		}
	}
}
