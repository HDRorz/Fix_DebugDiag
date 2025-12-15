namespace Microsoft.Diagnostics.Runtime;

public enum GCRootKind
{
	StaticVar = 0,
	ThreadStaticVar = 1,
	LocalVar = 2,
	Strong = 3,
	Weak = 4,
	Pinning = 5,
	Finalizer = 6,
	AsyncPinning = 7,
	Max = 7
}
