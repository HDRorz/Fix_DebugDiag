namespace DebugDiag.DbgEng;

public struct DEBUG_MODULE_PARAMETERS
{
	public ulong Base;

	public uint Size;

	public uint TimeDateStamp;

	public uint Checksum;

	public DEBUG_MODULE Flags;

	public DEBUG_SYMTYPE SymbolType;

	public uint ImageNameSize;

	public uint ModuleNameSize;

	public uint LoadedImageNameSize;

	public uint SymbolFileNameSize;

	public uint MappedImageNameSize;

	public unsafe fixed ulong Reserved[2];
}
