namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct COR_TYPE_LAYOUT
{
	public COR_TYPEID parentID;

	public int objectSize;

	public int numFields;

	public int boxOffset;

	public CorElementType type;
}
