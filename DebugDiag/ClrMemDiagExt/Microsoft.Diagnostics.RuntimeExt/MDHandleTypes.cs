namespace Microsoft.Diagnostics.RuntimeExt;

public enum MDHandleTypes
{
	MDHandle_WeakShort,
	MDHandle_WeakLong,
	MDHandle_Strong,
	MDHandle_Pinned,
	MDHandle_Variable,
	MDHandle_RefCount,
	MDHandle_Dependent,
	MDHandle_AsyncPinned,
	MDHandle_SizedRef
}
