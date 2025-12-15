namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopHeapInterface : ClrInterface
{
	private string _name;

	private ClrInterface _base;

	public override string Name => _name;

	public override ClrInterface BaseInterface => _base;

	public DesktopHeapInterface(string name, ClrInterface baseInterface)
	{
		_name = name;
		_base = baseInterface;
	}
}
