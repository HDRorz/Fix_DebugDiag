namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeFinalizerRoot : ClrRoot
{
	private string _name;

	private ClrType _type;

	private ClrAppDomain _appDomain;

	public override GCRootKind Kind => GCRootKind.Finalizer;

	public override ClrType Type => _type;

	public override string Name => _name;

	public override ClrAppDomain AppDomain => _appDomain;

	public NativeFinalizerRoot(ulong obj, ClrType type, ClrAppDomain domain, string name)
	{
		Object = obj;
		_name = name;
		_type = type;
		_appDomain = domain;
	}
}
