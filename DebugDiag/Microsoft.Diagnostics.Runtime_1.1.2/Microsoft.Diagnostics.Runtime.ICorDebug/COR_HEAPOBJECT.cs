using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct COR_HEAPOBJECT
{
	public ulong address;

	public ulong size;

	public COR_TYPEID type;
}
