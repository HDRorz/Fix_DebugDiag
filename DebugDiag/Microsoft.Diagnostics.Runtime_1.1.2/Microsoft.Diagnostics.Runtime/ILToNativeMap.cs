namespace Microsoft.Diagnostics.Runtime;

public struct ILToNativeMap
{
	public int ILOffset;

	public ulong StartAddress;

	public ulong EndAddress;

	private int _reserved;

	public override string ToString()
	{
		return $"{ILOffset,2:X} - [{StartAddress:X}-{EndAddress:X}]";
	}
}
