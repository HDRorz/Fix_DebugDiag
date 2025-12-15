using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public sealed class FileVersionInfo
{
	public string FileVersion { get; }

	public VersionInfo VersionInfo { get; }

	public string Comments { get; }

	internal unsafe FileVersionInfo(byte[] data, int dataLen)
	{
		FileVersion = "";
		string @string = Encoding.Unicode.GetString(data);
		FileVersion = GetDataString(@string, "FileVersion");
		Comments = GetDataString(@string, "Comments");
		fixed (byte* data2 = data)
		{
			VersionInfo = GetVersionInfo(data2, dataLen, @string);
		}
	}

	private unsafe static VersionInfo GetVersionInfo(byte* data, int dataLen, string dataAsString)
	{
		int num = dataAsString.IndexOf("VS_VERSION_INFO");
		if (num < 0)
		{
			return default(VersionInfo);
		}
		int num2 = (num + "VS_VERSION_INFO".Length) * 2;
		int minor = (ushort)Marshal.ReadInt16(new IntPtr(data + num2 + 12));
		ushort major = (ushort)Marshal.ReadInt16(new IntPtr(data + num2 + 14));
		int patch = (ushort)Marshal.ReadInt16(new IntPtr(data + num2 + 16));
		int revision = (ushort)Marshal.ReadInt16(new IntPtr(data + num2 + 18));
		return new VersionInfo(major, minor, revision, patch);
	}

	[Obsolete]
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

	public override string ToString()
	{
		return FileVersion;
	}
}
