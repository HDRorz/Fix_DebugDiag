namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopManagedWorkItem : ManagedWorkItem
{
	public override ulong Object { get; }

	public override ClrType Type { get; }

	public DesktopManagedWorkItem(ClrType type, ulong addr)
	{
		Type = type;
		Object = addr;
	}
}
