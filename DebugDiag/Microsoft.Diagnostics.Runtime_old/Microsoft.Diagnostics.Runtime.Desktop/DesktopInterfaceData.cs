namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopInterfaceData : ComInterfaceData
{
	private ulong _interface;

	private ClrType _type;

	public override ClrType Type => _type;

	public override ulong InterfacePointer => _interface;

	public DesktopInterfaceData(ClrType type, ulong ptr)
	{
		_type = type;
		_interface = ptr;
	}
}
