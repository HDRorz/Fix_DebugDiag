namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class MinidumpMemory64List
{
	private DumpPointer _streamPointer;

	public ulong Count => (ulong)_streamPointer.ReadInt64();

	public RVA64 BaseRva => _streamPointer.PtrToStructure<RVA64>(8u);

	public MinidumpMemory64List(DumpPointer streamPointer)
	{
		_streamPointer = streamPointer;
	}

	public MINIDUMP_MEMORY_DESCRIPTOR64 GetElement(uint idx)
	{
		uint offset = 16 + idx * 16;
		return _streamPointer.PtrToStructure<MINIDUMP_MEMORY_DESCRIPTOR64>(offset);
	}
}
