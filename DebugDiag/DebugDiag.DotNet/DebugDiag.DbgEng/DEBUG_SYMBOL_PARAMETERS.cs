namespace DebugDiag.DbgEng;

public struct DEBUG_SYMBOL_PARAMETERS
{
	public ulong Module;

	public uint TypeId;

	public uint ParentSymbol;

	public uint SubElements;

	public uint Flags;

	public ulong Reserved;
}
