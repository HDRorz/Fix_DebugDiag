namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V4FieldInfo : IFieldInfo
{
	private short _wNumInstanceFields;

	private short _wNumStaticFields;

	private short _wNumThreadStaticFields;

	private ulong _addrFirstField;

	private short _wContextStaticOffset;

	private short _wContextStaticsSize;

	public uint InstanceFields => (uint)_wNumInstanceFields;

	public uint StaticFields => (uint)_wNumStaticFields;

	public uint ThreadStaticFields => (uint)_wNumThreadStaticFields;

	public ulong FirstField => _addrFirstField;
}
