using System.IO;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal static class HelperExtensions
{
	public static string GetFilename(this Stream stream)
	{
		if (!(stream is FileStream fileStream))
		{
			return null;
		}
		return fileStream.Name;
	}
}
