namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct MINIDUMP_MEMORY_DESCRIPTOR
{
	public const int SizeOf = 16;

	private readonly ulong _startofmemoryrange;

	public MINIDUMP_LOCATION_DESCRIPTOR Memory;

	public ulong StartOfMemoryRange => DumpNative.ZeroExtendAddress(_startofmemoryrange);
}
