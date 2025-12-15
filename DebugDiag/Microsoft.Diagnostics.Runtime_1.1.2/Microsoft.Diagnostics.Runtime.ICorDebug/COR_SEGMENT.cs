using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct COR_SEGMENT
{
	public ulong start;

	public ulong end;

	public CorDebugGenerationTypes type;

	public uint heap;
}
