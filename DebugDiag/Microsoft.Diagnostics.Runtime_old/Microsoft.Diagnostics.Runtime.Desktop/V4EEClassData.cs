namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V4EEClassData : IEEClassData, IFieldInfo
{
	public ulong methodTable;

	public ulong module;

	public short wNumVtableSlots;

	public short wNumMethodSlots;

	public short wNumInstanceFields;

	public short wNumStaticFields;

	public short wNumThreadStaticFields;

	public uint dwClassDomainNeutralIndex;

	public uint dwAttrClass;

	public uint token;

	public ulong addrFirstField;

	public short wContextStaticOffset;

	public short wContextStaticsSize;

	public ulong Module => module;

	ulong IEEClassData.MethodTable => methodTable;

	public uint InstanceFields => (uint)wNumInstanceFields;

	public uint StaticFields => (uint)wNumStaticFields;

	public uint ThreadStaticFields => 0u;

	public ulong FirstField => addrFirstField;
}
