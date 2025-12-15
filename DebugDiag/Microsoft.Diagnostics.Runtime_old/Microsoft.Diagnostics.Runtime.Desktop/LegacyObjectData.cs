namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyObjectData : IObjectData
{
	private ulong _eeClass;

	private ulong _methodTable;

	private uint _objectType;

	private uint _size;

	private ulong _elementTypeHandle;

	private uint _elementType;

	private uint _dwRank;

	private uint _dwNumComponents;

	private uint _dwComponentSize;

	private ulong _arrayDataPtr;

	private ulong _arrayBoundsPtr;

	private ulong _arrayLowerBoundsPtr;

	public ClrElementType ElementType => (ClrElementType)_elementType;

	public ulong ElementTypeHandle => _elementTypeHandle;

	public ulong RCW => 0uL;

	public ulong CCW => 0uL;

	public ulong DataPointer => _arrayDataPtr;
}
