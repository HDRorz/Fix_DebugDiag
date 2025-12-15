using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_THUNK_DATA32
{
	[FieldOffset(0)]
	public uint ForwarderString;

	[FieldOffset(0)]
	public uint Function;

	[FieldOffset(0)]
	public uint Ordinal;

	[FieldOffset(0)]
	public uint AddressOfData;
}
