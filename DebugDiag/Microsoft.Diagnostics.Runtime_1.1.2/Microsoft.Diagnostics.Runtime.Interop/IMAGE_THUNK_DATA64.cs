using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_THUNK_DATA64
{
	[FieldOffset(0)]
	public ulong ForwarderString;

	[FieldOffset(0)]
	public ulong Function;

	[FieldOffset(0)]
	public ulong Ordinal;

	[FieldOffset(0)]
	public ulong AddressOfData;
}
