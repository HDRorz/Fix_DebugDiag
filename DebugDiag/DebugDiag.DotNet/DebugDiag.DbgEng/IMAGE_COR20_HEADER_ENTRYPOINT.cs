using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_COR20_HEADER_ENTRYPOINT
{
	[FieldOffset(0)]
	private uint Token;

	[FieldOffset(0)]
	private uint RVA;
}
