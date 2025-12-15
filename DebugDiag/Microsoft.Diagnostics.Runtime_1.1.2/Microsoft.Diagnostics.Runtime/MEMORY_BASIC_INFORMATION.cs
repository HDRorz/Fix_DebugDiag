using System;

namespace Microsoft.Diagnostics.Runtime;

internal struct MEMORY_BASIC_INFORMATION
{
	public IntPtr Address;

	public IntPtr AllocationBase;

	public uint AllocationProtect;

	public IntPtr RegionSize;

	public uint State;

	public uint Protect;

	public uint Type;

	public ulong BaseAddress => (ulong)(long)Address;

	public ulong Size => (ulong)(long)RegionSize;
}
