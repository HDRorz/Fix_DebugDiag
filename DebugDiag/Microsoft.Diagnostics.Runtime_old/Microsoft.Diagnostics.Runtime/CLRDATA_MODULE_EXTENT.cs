namespace Microsoft.Diagnostics.Runtime;

internal struct CLRDATA_MODULE_EXTENT
{
	public ulong baseAddress;

	public uint length;

	public ModuleExtentType type;
}
