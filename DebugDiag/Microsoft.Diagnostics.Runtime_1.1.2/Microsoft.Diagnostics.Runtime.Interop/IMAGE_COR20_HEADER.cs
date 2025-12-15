namespace Microsoft.Diagnostics.Runtime.Interop;

public struct IMAGE_COR20_HEADER
{
	public uint cb;

	public ushort MajorRuntimeVersion;

	public ushort MinorRuntimeVersion;

	public IMAGE_DATA_DIRECTORY MetaData;

	public uint Flags;

	public IMAGE_COR20_HEADER_ENTRYPOINT EntryPoint;

	public IMAGE_DATA_DIRECTORY Resources;

	public IMAGE_DATA_DIRECTORY StrongNameSignature;

	public IMAGE_DATA_DIRECTORY CodeManagerTable;

	public IMAGE_DATA_DIRECTORY VTableFixups;

	public IMAGE_DATA_DIRECTORY ExportAddressTableJumps;

	public IMAGE_DATA_DIRECTORY ManagedNativeHeader;
}
