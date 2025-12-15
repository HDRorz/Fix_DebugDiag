using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_COR20_HEADER_ENTRYPOINT
{
	[FieldOffset(0)]
	public readonly uint Token;

	[FieldOffset(0)]
	public readonly uint RVA;
}
