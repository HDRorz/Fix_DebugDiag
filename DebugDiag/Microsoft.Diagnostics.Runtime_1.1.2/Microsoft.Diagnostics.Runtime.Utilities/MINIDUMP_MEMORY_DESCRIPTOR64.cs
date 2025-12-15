namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct MINIDUMP_MEMORY_DESCRIPTOR64
{
	public const int SizeOf = 16;

	private readonly ulong _startofmemoryrange;

	public ulong DataSize;

	public ulong StartOfMemoryRange => DumpNative.ZeroExtendAddress(_startofmemoryrange);
}
