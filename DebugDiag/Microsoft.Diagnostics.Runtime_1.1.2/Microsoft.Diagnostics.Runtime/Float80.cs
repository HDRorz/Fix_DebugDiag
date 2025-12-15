using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Float80
{
	[FieldOffset(0)]
	public ulong Mantissa;

	[FieldOffset(8)]
	public ushort Exponent;
}
