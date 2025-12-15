namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrRoot
{
	public abstract GCRootKind Kind { get; }

	public virtual string Name => "";

	public abstract ClrType Type { get; }

	public virtual ulong Object { get; protected set; }

	public virtual ulong Address { get; protected set; }

	public virtual ClrAppDomain AppDomain => null;

	public virtual ClrThread Thread => null;

	public virtual bool IsInterior => false;

	public virtual bool IsPinned => false;

	public virtual bool IsPossibleFalsePositive => false;

	public override string ToString()
	{
		return $"GCRoot {Address:X8}->{Object:X8} {Name}";
	}
}
