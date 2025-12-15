namespace Microsoft.Diagnostics.Runtime.Utilities;

public class FindBinaryEventArgs
{
	public string FileName { get; private set; }

	public int TimeStamp { get; private set; }

	public int FileSize { get; private set; }

	public string Result { get; set; }

	internal FindBinaryEventArgs(string fileName, int timeStamp, int fileSize)
	{
		FileName = fileName;
		TimeStamp = timeStamp;
		FileSize = fileSize;
	}
}
