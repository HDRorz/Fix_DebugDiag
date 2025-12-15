namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct COR_ARRAY_LAYOUT
{
	public COR_TYPEID componentID;

	public CorElementType componentType;

	public int firstElementOffset;

	public int elementSize;

	public int countOffset;

	public int rankSize;

	public int numRanks;

	public int rankOffset;
}
