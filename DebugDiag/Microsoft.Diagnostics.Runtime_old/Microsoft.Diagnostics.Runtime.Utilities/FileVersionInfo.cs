namespace Microsoft.Diagnostics.Runtime.Utilities;

public sealed class FileVersionInfo
{
	public string FileVersion { get; private set; }

	public string Comments { get; private set; }

	internal unsafe FileVersionInfo(byte* data, int dataLen)
	{
		FileVersion = "";
		if (dataLen > 92)
		{
			string dataAsString = new string((char*)(data + 92), 0, (dataLen - 92) / 2);
			FileVersion = GetDataString(dataAsString, "FileVersion");
			Comments = GetDataString(dataAsString, "Comments");
		}
	}

	private static string GetDataString(string dataAsString, string fileVersionKey)
	{
		int num = dataAsString.IndexOf(fileVersionKey);
		if (num >= 0)
		{
			int num2 = num + fileVersionKey.Length;
			do
			{
				num2++;
				if (num2 >= dataAsString.Length)
				{
					return null;
				}
			}
			while (dataAsString[num2] == '\0');
			int num3 = dataAsString.IndexOf('\0', num2);
			if (num3 < 0)
			{
				return null;
			}
			return dataAsString.Substring(num2, num3 - num2);
		}
		return null;
	}
}
