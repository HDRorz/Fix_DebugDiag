using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct COR_HEAPINFO
{
	public uint areGCStructuresValid;

	public uint pointerSize;

	public uint numHeaps;

	public uint concurrent;

	public CorDebugGCType gcType;
}
