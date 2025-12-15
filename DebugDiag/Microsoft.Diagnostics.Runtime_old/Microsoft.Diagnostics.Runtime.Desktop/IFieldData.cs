namespace Microsoft.Diagnostics.Runtime.Desktop;

internal interface IFieldData
{
	uint CorElementType { get; }

	uint SigType { get; }

	ulong TypeMethodTable { get; }

	ulong Module { get; }

	uint TypeToken { get; }

	uint FieldToken { get; }

	ulong EnclosingMethodTable { get; }

	uint Offset { get; }

	bool IsThreadLocal { get; }

	bool bIsContextLocal { get; }

	bool bIsStatic { get; }

	ulong nextField { get; }
}
