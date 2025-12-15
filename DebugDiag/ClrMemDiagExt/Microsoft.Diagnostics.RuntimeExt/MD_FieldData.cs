using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
public struct MD_FieldData
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string name;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string type;

	public int corElementType;

	public int offset;

	public int size;

	public ulong value;
}
