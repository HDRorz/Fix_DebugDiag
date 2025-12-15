namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeObjectData
{
	public ulong MethodTable;

	public DacpObjectType ObjectType;

	public uint Size;

	public ulong ElementTypeHandle;

	public uint ElementType;

	public uint dwRank;

	public uint dwNumComponents;

	public uint dwComponentSize;

	public ulong ArrayDataPtr;

	public ulong ArrayBoundsPtr;

	public ulong ArrayLowerBoundsPtr;
}
