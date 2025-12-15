namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct COR_FIELD
{
	public int token;

	public int offset;

	public COR_TYPEID id;

	public CorElementType fieldType;
}
