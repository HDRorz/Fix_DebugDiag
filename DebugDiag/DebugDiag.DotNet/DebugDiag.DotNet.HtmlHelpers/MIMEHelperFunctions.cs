#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DebugDiag.DotNet.HtmlHelpers;

internal static class MIMEHelperFunctions
{
	private static readonly byte[] newLine = new byte[3] { 61, 13, 10 };

	private static readonly byte[] spaceEndLine = new byte[6] { 61, 50, 48, 61, 13, 10 };

	private static readonly byte[] tabEndLine = new byte[6] { 61, 48, 57, 61, 13, 10 };

	/// <summary>
	/// Enodes a String into a UTF-8 based quoted printable representation
	/// </summary>
	/// <param name="source">string to encode</param>
	/// <param name="encoded">stream where the encoded data is added</param>
	/// <returns>stream containing the encoded bytes</returns>
	internal static void QPEncode(MemoryStream source, BinaryWriter encoded)
	{
		if (source == null || source.Length == 0L)
		{
			return;
		}
		if (encoded == null)
		{
			throw new ArgumentException("The BinaryWriter parameter object cannot be null");
		}
		Encoding encoding = Encoding.GetEncoding("utf-8");
		byte[] buffer = source.GetBuffer();
		long length = source.Length;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		string text = string.Empty;
		try
		{
			byte[] array = buffer;
			foreach (byte b in array)
			{
				if ((b > 61 && b < 127) || (b > 31 && b < 61) || b == 9)
				{
					num3++;
					num++;
				}
				else
				{
					text = $"={b:X2}";
					num += 3;
				}
				if (!string.IsNullOrEmpty(text))
				{
					if (num3 > 0)
					{
						encoded.Write(buffer, num2, num3);
					}
					num2 += num3 + 1;
					num3 = 0;
					encoded.Write(encoding.GetBytes(text), 0, 3);
					text = string.Empty;
					if (num > 68)
					{
						encoded.Write(newLine, 0, 3);
						num = 0;
					}
				}
				else if (num > 68)
				{
					if ((b == 32 || b == 9) && num2 + num3 < buffer.Length)
					{
						if (num3 > 1)
						{
							encoded.Write(buffer, num2, num3 - 1);
						}
						if (b == 32)
						{
							encoded.Write(spaceEndLine, 0, 6);
						}
						else
						{
							encoded.Write(tabEndLine, 0, 6);
						}
						num2 += num3;
						num3 = 0;
						num = 0;
					}
					else
					{
						if (num3 > 0)
						{
							encoded.Write(buffer, num2, num3);
						}
						encoded.Write(newLine, 0, 3);
						num2 += num3;
						num3 = 0;
						num = 0;
					}
				}
				if (num2 + num3 > length)
				{
					break;
				}
			}
			if (num3 > 0)
			{
				encoded.Write(buffer, num2, num3);
			}
		}
		catch (IOException ex)
		{
			Trace.TraceError("Failed to QPEncode the string due to a IO exception");
			throw ex;
		}
		catch (ObjectDisposedException ex2)
		{
			Trace.TraceError("Failed to QPEncode the string due to the destination stream is closed");
			throw ex2;
		}
	}

	internal static byte[] GetEncodedBytes(string source)
	{
		return Encoding.GetEncoding("utf-8").GetBytes(source);
	}

	internal static byte[] FileHeader()
	{
		return GetEncodedBytes("MIME-Version: 1.0\r\nContent-Type: multipart/related;\r\n\tboundary=\"__ANALYZER_REPORT_BOUNDARY__\";\r\n\ttype=\"text/html\"\r\n\r\nThis is a multi-part message in MIME format.\r\n\r\n--__ANALYZER_REPORT_BOUNDARY__\r\nContent-Type: text/html;\r\n\tcharset=\"utf-8\"\r\nContent-Transfer-Encoding: quoted-printable\r\nContent-Location: file:///C:/Report.htm\r\n\r\n");
	}

	internal static byte[] ImageHeader(string ImageName)
	{
		return GetEncodedBytes("\r\n\r\n--__ANALYZER_REPORT_BOUNDARY__\r\nContent-Type: image/gif\r\nContent-Transfer-Encoding: base64\r\n" + $"Content-Location: file:///C:/res/{ImageName}\r\n\r\n");
	}

	internal static byte[] FontHeader(string FontName)
	{
		return GetEncodedBytes("\r\n\r\n--__ANALYZER_REPORT_BOUNDARY__\r\nContent-Type: application/x-woff\r\nContent-Transfer-Encoding: base64\r\n" + $"Content-Location: file:///C:/res/{FontName}\r\n\r\n");
	}

	internal static byte[] TextHeader(string fileName, string container, string contentType)
	{
		return GetEncodedBytes("\r\n\r\n--__ANALYZER_REPORT_BOUNDARY__\r\n" + $"Content-Type: {contentType};\r\n\tcharset=\"utf-8\"\r\n" + "Content-Transfer-Encoding: quoted-printable\r\n" + $"Content-Location: file:///C:/{container}/{fileName}\r\n\r\n");
	}

	internal static byte[] FileFooter()
	{
		return GetEncodedBytes("\r\n\r\n--__ANALYZER_REPORT_BOUNDARY__--");
	}
}
