using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct FieldData : IFieldData
{
	public readonly uint ElementType;

	public readonly uint SigType;

	public readonly ulong TypeMethodTable;

	public readonly ulong TypeModule;

	public readonly uint MDType;

	public readonly uint MDField;

	public readonly ulong MTOfEnclosingClass;

	public readonly uint Offset;

	public readonly uint IsThreadLocal;

	public readonly uint IsContextLocal;

	public readonly uint IsStatic;

	public readonly ulong NextField;

	uint IFieldData.CorElementType => ElementType;

	uint IFieldData.SigType => SigType;

	ulong IFieldData.TypeMethodTable => TypeMethodTable;

	ulong IFieldData.Module => TypeModule;

	uint IFieldData.TypeToken => MDType;

	uint IFieldData.FieldToken => MDField;

	ulong IFieldData.EnclosingMethodTable => MTOfEnclosingClass;

	uint IFieldData.Offset => Offset;

	bool IFieldData.IsThreadLocal => IsThreadLocal != 0;

	bool IFieldData.IsContextLocal => IsContextLocal != 0;

	bool IFieldData.IsStatic => IsStatic != 0;

	ulong IFieldData.NextField => NextField;
}
