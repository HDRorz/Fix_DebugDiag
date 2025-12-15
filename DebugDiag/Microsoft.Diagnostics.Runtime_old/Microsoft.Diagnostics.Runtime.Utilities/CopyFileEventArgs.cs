using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class CopyFileEventArgs
{
	public string Source { get; private set; }

	public string Destination { get; private set; }

	public Stream Stream { get; private set; }

	public long Size { get; private set; }

	public bool IsCancelled { get; private set; }

	public bool IsComplete { get; private set; }

	public void Cancel()
	{
		IsCancelled = true;
	}

	public void Complete()
	{
		IsComplete = true;
	}

	public CopyFileEventArgs(string src, string dst, Stream stream, long size)
	{
		Source = src;
		Destination = dst;
		Stream = stream;
		Size = size;
	}
}
