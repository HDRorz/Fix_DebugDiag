using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_COR20_HEADER_ENTRYPOINT
{
	[FieldOffset(0)]
	private uint _token;

	[FieldOffset(0)]
	private uint _RVA;
}
