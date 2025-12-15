using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[StructLayout(LayoutKind.Sequential)]
public class EXT_TYPED_DATA
{
	public _EXT_TDOP Operation;

	public uint Flags;

	public _DEBUG_TYPED_DATA InData;

	public _DEBUG_TYPED_DATA OutData;

	public uint InStrIndex;

	public uint In32;

	public uint Out32;

	public ulong In64;

	public ulong Out64;

	public uint StrBufferIndex;

	public uint StrBufferChars;

	public uint StrCharsNeeded;

	public uint DataBufferIndex;

	public uint DataBufferBytes;

	public uint DataBytesNeeded;

	public uint Status;
}
