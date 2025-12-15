using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public struct VirtualQueryData
{
	public ulong BaseAddress;

	public ulong Size;

	public VirtualQueryData(ulong addr, ulong size)
	{
		BaseAddress = addr;
		Size = size;
	}
}
