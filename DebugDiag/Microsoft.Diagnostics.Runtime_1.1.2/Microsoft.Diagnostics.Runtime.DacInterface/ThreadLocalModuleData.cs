namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct ThreadLocalModuleData
{
	public readonly ulong ThreadAddress;

	public readonly ulong ModuleIndex;

	public readonly ulong ClassData;

	public readonly ulong DynamicClassTable;

	public readonly ulong GCStaticDataStart;

	public readonly ulong NonGCStaticDataStart;
}
