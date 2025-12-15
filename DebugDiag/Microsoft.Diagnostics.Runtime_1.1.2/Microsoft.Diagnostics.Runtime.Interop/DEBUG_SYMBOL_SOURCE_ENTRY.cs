namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_SYMBOL_SOURCE_ENTRY
{
	private readonly ulong _moduleBase;

	private readonly ulong _offset;

	private readonly ulong _fileNameId;

	private readonly ulong _engineInternal;

	private readonly uint _size;

	private readonly uint _flags;

	private readonly uint _fileNameSize;

	private readonly uint _startLine;

	private readonly uint _endLine;

	private readonly uint _startColumn;

	private readonly uint _endColumn;

	private readonly uint _reserved;
}
