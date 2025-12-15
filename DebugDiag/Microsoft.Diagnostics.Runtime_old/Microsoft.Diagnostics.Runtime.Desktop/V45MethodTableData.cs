using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V45MethodTableData : IMethodTableData
{
	public uint bIsFree;

	public ulong module;

	public ulong eeClass;

	public ulong parentMethodTable;

	public ushort wNumInterfaces;

	public ushort wNumMethods;

	public ushort wNumVtableSlots;

	public ushort wNumVirtuals;

	public uint baseSize;

	public uint componentSize;

	public uint token;

	public uint dwAttrClass;

	public uint isShared;

	public uint isDynamic;

	public uint containsPointers;

	public bool ContainsPointers => containsPointers != 0;

	public uint BaseSize => baseSize;

	public uint ComponentSize => componentSize;

	public ulong EEClass => eeClass;

	public bool Free => bIsFree != 0;

	public ulong Parent => parentMethodTable;

	public bool Shared => isShared != 0;

	public uint NumMethods => wNumMethods;

	public ulong ElementTypeHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}
