namespace Microsoft.Diagnostics.Runtime;

public readonly struct ClrObjectReference
{
	public int FieldOffset { get; }

	public ulong Address { get; }

	public ClrType TargetType { get; }

	public ClrObject Object => new ClrObject(Address, TargetType);

	public ClrObjectReference(int fieldOffset, ulong address, ClrType targetType)
	{
		FieldOffset = fieldOffset;
		Address = address;
		TargetType = targetType;
	}
}
