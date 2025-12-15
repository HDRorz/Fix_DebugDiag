namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct MINIDUMP_LOCATION_DESCRIPTOR
{
	public uint DataSize;

	public RVA Rva;

	public bool IsNull
	{
		get
		{
			if (DataSize != 0)
			{
				return Rva.IsNull;
			}
			return true;
		}
	}
}
