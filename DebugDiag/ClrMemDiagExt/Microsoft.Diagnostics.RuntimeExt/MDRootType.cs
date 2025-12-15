namespace Microsoft.Diagnostics.RuntimeExt;

public enum MDRootType
{
	MDRoot_StaticVar,
	MDRoot_ThreadStaticVar,
	MDRoot_LocalVar,
	MDRoot_Strong,
	MDRoot_Weak,
	MDRoot_Pinning,
	MDRoot_Finalizer,
	MDRoot_AsyncPinning
}
