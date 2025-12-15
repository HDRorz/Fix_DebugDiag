namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyObjectData : IObjectData
{
	public readonly ulong EEClass;

	public readonly ulong MethodTable;

	public readonly uint ObjectType;

	public readonly uint Size;

	public readonly ulong ElementTypeHandle;

	public readonly uint ElementType;

	public readonly uint Rank;

	public readonly uint NumComponents;

	public readonly uint ComponentSize;

	public readonly ulong ArrayDataPtr;

	public readonly ulong ArrayBoundsPtr;

	public readonly ulong ArrayLowerBoundsPtr;

	ClrElementType IObjectData.ElementType => (ClrElementType)ElementType;

	ulong IObjectData.ElementTypeHandle => ElementTypeHandle;

	ulong IObjectData.RCW => 0uL;

	ulong IObjectData.CCW => 0uL;

	ulong IObjectData.DataPointer => ArrayDataPtr;
}
