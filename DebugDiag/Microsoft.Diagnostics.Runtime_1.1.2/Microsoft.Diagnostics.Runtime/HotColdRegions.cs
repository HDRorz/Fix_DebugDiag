namespace Microsoft.Diagnostics.Runtime;

public class HotColdRegions
{
	public ulong HotStart { get; internal set; }

	public uint HotSize { get; internal set; }

	public ulong ColdStart { get; internal set; }

	public uint ColdSize { get; internal set; }
}
