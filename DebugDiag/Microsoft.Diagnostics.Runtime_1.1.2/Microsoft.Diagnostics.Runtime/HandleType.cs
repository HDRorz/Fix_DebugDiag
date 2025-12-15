namespace Microsoft.Diagnostics.Runtime;

public enum HandleType
{
	WeakShort = 0,
	WeakLong = 1,
	Strong = 2,
	Pinned = 3,
	RefCount = 5,
	Dependent = 6,
	AsyncPinned = 7,
	SizedRef = 8
}
