using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeMethodTableData : IMethodTableData
{
	public uint objectType;

	public ulong canonicalMethodTable;

	public ulong parentMethodTable;

	public ushort wNumInterfaces;

	public ushort wNumVtableSlots;

	public uint baseSize;

	public uint componentSize;

	public uint sizeofMethodTable;

	public uint containsPointers;

	public ulong elementTypeHandle;

	public bool ContainsPointers => containsPointers != 0;

	public uint BaseSize => baseSize;

	public uint ComponentSize => componentSize;

	public ulong EEClass => canonicalMethodTable;

	public bool Free => objectType == 0;

	public ulong Parent => parentMethodTable;

	public bool Shared => false;

	public uint NumMethods => wNumVtableSlots;

	public ulong ElementTypeHandle => elementTypeHandle;
}
