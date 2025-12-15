namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V2EEClassData : IEEClassData, IFieldInfo
{
	public readonly ulong MethodTable;

	public readonly ulong Module;

	public readonly short NumVTableSlots;

	public readonly short NumMethodSlots;

	public readonly short NumInstanceFields;

	public readonly short NumStaticFields;

	public readonly uint ClassDomainNeutralIndex;

	public readonly uint AttrClass;

	public readonly uint Token;

	public readonly ulong AddrFirstField;

	public readonly short ThreadStaticOffset;

	public readonly short ThreadStaticsSize;

	public readonly short ContextStaticOffset;

	public readonly short ContextStaticsSize;

	ulong IEEClassData.Module => Module;

	ulong IEEClassData.MethodTable => MethodTable;

	uint IFieldInfo.InstanceFields => (uint)NumInstanceFields;

	uint IFieldInfo.StaticFields => (uint)NumStaticFields;

	uint IFieldInfo.ThreadStaticFields => 0u;

	ulong IFieldInfo.FirstField => AddrFirstField;
}
