namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class MinidumpMemoryList
{
	private DumpPointer _streamPointer;

	public uint Count => (uint)_streamPointer.ReadInt32();

	public MinidumpMemoryList(DumpPointer streamPointer)
	{
		_streamPointer = streamPointer;
	}

	public MINIDUMP_MEMORY_DESCRIPTOR GetElement(uint idx)
	{
		uint offset = 4 + idx * 16;
		return _streamPointer.PtrToStructure<MINIDUMP_MEMORY_DESCRIPTOR>(offset);
	}
}
