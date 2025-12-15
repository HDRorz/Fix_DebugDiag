namespace DebugDiag.DbgEng;

public struct DEBUG_SYMBOL_SOURCE_ENTRY
{
	private ulong ModuleBase;

	private ulong Offset;

	private ulong FileNameId;

	private ulong EngineInternal;

	private uint Size;

	private uint Flags;

	private uint FileNameSize;

	private uint StartLine;

	private uint EndLine;

	private uint StartColumn;

	private uint EndColumn;

	private uint Reserved;
}
