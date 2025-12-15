namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct MINIDUMP_LOCATION_DESCRIPTOR64
{
	public ulong DataSize;

	public RVA64 Rva;
}
