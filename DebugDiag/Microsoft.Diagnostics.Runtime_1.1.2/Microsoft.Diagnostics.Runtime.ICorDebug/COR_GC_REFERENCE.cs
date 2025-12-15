namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct COR_GC_REFERENCE
{
	public ICorDebugAppDomain Domain;

	public ICorDebugValue Location;

	public CorGCReferenceType Type;

	public ulong ExtraData;
}
