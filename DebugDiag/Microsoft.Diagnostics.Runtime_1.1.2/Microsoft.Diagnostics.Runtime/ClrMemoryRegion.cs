namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrMemoryRegion
{
	public ulong Address { get; set; }

	public ulong Size { get; set; }

	public ClrMemoryRegionType Type { get; set; }

	public abstract ClrAppDomain AppDomain { get; }

	public abstract string Module { get; }

	public abstract int HeapNumber { get; set; }

	public abstract GCSegmentType GCSegmentType { get; set; }

	public abstract string ToString(bool detailed);

	public override string ToString()
	{
		return ToString(detailed: false);
	}
}
