using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyMethodTableData : IMethodTableData
{
	public readonly uint IsFree;

	public readonly ulong EEClass;

	public readonly ulong ParentMethodTable;

	public readonly ushort NumInterfaces;

	public readonly ushort NumVtableSlots;

	public readonly uint BaseSize;

	public readonly uint ComponentSize;

	public readonly uint IsShared;

	public readonly uint SizeofMethodTable;

	public readonly uint IsDynamic;

	public readonly uint ContainsPointers;

	bool IMethodTableData.ContainsPointers => ContainsPointers != 0;

	uint IMethodTableData.BaseSize => BaseSize;

	uint IMethodTableData.ComponentSize => ComponentSize;

	ulong IMethodTableData.EEClass => EEClass;

	bool IMethodTableData.Free => IsFree != 0;

	ulong IMethodTableData.Parent => ParentMethodTable;

	bool IMethodTableData.Shared => IsShared != 0;

	uint IMethodTableData.NumMethods => NumVtableSlots;

	ulong IMethodTableData.ElementTypeHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	uint IMethodTableData.Token => 0u;

	ulong IMethodTableData.Module => 0uL;
}
