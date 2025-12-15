using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
public struct MD_Reference
{
	public ulong address;

	public int offset;
}
