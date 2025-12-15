using System;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct MethodTableData : IMethodTableData
{
	public readonly uint IsFree;

	public readonly ulong Module;

	public readonly ulong EEClass;

	public readonly ulong ParentMethodTable;

	public readonly ushort NumInterfaces;

	public readonly ushort NumMethods;

	public readonly ushort NumVtableSlots;

	public readonly ushort NumVirtuals;

	public readonly uint BaseSize;

	public readonly uint ComponentSize;

	public readonly uint Token;

	public readonly uint AttrClass;

	public readonly uint Shared;

	public readonly uint Dynamic;

	public readonly uint ContainsPointers;

	uint IMethodTableData.Token => Token;

	ulong IMethodTableData.Module => Module;

	bool IMethodTableData.ContainsPointers => ContainsPointers != 0;

	uint IMethodTableData.BaseSize => BaseSize;

	uint IMethodTableData.ComponentSize => ComponentSize;

	ulong IMethodTableData.EEClass => EEClass;

	bool IMethodTableData.Free => IsFree != 0;

	ulong IMethodTableData.Parent => ParentMethodTable;

	bool IMethodTableData.Shared => Shared != 0;

	uint IMethodTableData.NumMethods => NumMethods;

	ulong IMethodTableData.ElementTypeHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}
