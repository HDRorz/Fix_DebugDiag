using System.IO;

namespace Microsoft.Diagnostics.Runtime;

public class MemoryReadException : IOException
{
	public ulong Address { get; private set; }

	public MemoryReadException(ulong address)
		: base($"Could not read memory at {address:x}.")
	{
	}
}
