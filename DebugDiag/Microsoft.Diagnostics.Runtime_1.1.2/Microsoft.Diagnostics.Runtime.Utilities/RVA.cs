namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct RVA
{
	public uint Value;

	public bool IsNull => Value == 0;
}
