namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrField
{
	public abstract string Name { get; }

	public abstract uint Token { get; }

	public abstract ClrType Type { get; }

	public abstract ClrElementType ElementType { get; }

	public virtual bool IsPrimitive => ElementType.IsPrimitive();

	public virtual bool IsValueClass => ElementType.IsValueClass();

	public virtual bool IsObjectReference => ElementType.IsObjectReference();

	public abstract int Size { get; }

	public abstract bool IsPublic { get; }

	public abstract bool IsPrivate { get; }

	public abstract bool IsInternal { get; }

	public abstract bool IsProtected { get; }

	public abstract bool HasSimpleValue { get; }

	public virtual int Offset => -1;

	public override string ToString()
	{
		ClrType type = Type;
		if (type != null)
		{
			return type.Name + " " + Name;
		}
		return Name;
	}
}
