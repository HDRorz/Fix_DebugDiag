using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_IMPORT_DESCRIPTOR
{
	[FieldOffset(0)]
	public uint Characteristics;

	[FieldOffset(0)]
	public uint OriginalFirstThunk;

	[FieldOffset(4)]
	public uint TimeDateStamp;

	[FieldOffset(8)]
	public uint ForwarderChain;

	[FieldOffset(12)]
	public uint Name;

	[FieldOffset(16)]
	public uint FirstThunk;
}
