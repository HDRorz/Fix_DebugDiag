namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeStackRoot : ClrRoot
{
	private string _name;

	private ClrType _type;

	private ClrAppDomain _appDomain;

	private ClrThread _thread;

	private bool _pinned;

	private bool _interior;

	public override GCRootKind Kind => GCRootKind.LocalVar;

	public override ClrType Type => _type;

	public override bool IsPinned => _pinned;

	public override bool IsInterior => _interior;

	public override ClrAppDomain AppDomain => _appDomain;

	public override string Name => _name;

	public override ClrThread Thread => _thread;

	public NativeStackRoot(ClrThread thread, ulong addr, ulong obj, string name, ClrType type, ClrAppDomain domain, bool pinned, bool interior)
	{
		Address = addr;
		Object = obj;
		_name = name;
		_type = type;
		_appDomain = domain;
		_pinned = pinned;
		_interior = interior;
		_thread = thread;
	}
}
