namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct CodeHeaderData
{
	public readonly ulong GCInfo;

	public readonly uint JITType;

	public readonly ulong MethodDesc;

	public readonly ulong MethodStart;

	public readonly uint MethodSize;

	public readonly ulong ColdRegionStart;

	public readonly uint ColdRegionSize;

	public readonly uint HotRegionSize;
}
