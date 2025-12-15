using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyMethodTableData : IMethodTableData
{
	public uint bIsFree;

	public ulong eeClass;

	public ulong parentMethodTable;

	public ushort wNumInterfaces;

	public ushort wNumVtableSlots;

	public uint baseSize;

	public uint componentSize;

	public uint isShared;

	public uint sizeofMethodTable;

	public uint isDynamic;

	public uint containsPointers;

	public bool ContainsPointers => containsPointers != 0;

	public uint BaseSize => baseSize;

	public uint ComponentSize => componentSize;

	public ulong EEClass => eeClass;

	public bool Free => bIsFree != 0;

	public ulong Parent => parentMethodTable;

	public bool Shared => isShared != 0;

	public uint NumMethods => wNumVtableSlots;

	public ulong ElementTypeHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}
