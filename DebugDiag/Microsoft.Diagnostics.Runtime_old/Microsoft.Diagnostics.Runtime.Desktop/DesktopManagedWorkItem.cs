namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopManagedWorkItem : ManagedWorkItem
{
	private ClrType _type;

	private ulong _addr;

	public override ulong Object => _addr;

	public override ClrType Type => _type;

	public DesktopManagedWorkItem(ClrType type, ulong addr)
	{
		_type = type;
		_addr = addr;
	}
}
