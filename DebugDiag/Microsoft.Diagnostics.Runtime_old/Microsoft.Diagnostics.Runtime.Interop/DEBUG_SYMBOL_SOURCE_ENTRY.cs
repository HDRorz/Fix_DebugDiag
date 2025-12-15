namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_SYMBOL_SOURCE_ENTRY
{
	private ulong _moduleBase;

	private ulong _offset;

	private ulong _fileNameId;

	private ulong _engineInternal;

	private uint _size;

	private uint _flags;

	private uint _fileNameSize;

	private uint _startLine;

	private uint _endLine;

	private uint _startColumn;

	private uint _endColumn;

	private uint _reserved;
}
