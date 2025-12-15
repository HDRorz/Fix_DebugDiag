namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public enum CorGCReferenceType
{
	CorHandleStrong = 1,
	CorHandleStrongPinning = 2,
	CorHandleWeakShort = 4,
	CorHandleWeakLong = 8,
	CorHandleWeakRefCount = 16,
	CorHandleStrongRefCount = 32,
	CorHandleStrongDependent = 64,
	CorHandleStrongAsyncPinned = 128,
	CorHandleStrongSizedByref = 256,
	CorReferenceStack = -2147483647,
	CorReferenceFinalizer = int.MinValue,
	CorHandleStrongOnly = 483,
	CorHandleWeakOnly = 28,
	CorHandleAll = int.MaxValue
}
