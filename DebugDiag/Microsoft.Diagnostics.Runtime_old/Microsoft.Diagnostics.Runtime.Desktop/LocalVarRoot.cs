namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class LocalVarRoot : ClrRoot
{
	private bool _pinned;

	private bool _falsePos;

	private bool _interior;

	private ClrThread _thread;

	private ClrType _type;

	private ClrAppDomain _domain;

	public override ClrAppDomain AppDomain => _domain;

	public override ClrThread Thread => _thread;

	public override bool IsPossibleFalsePositive => _falsePos;

	public override string Name => "local var";

	public override bool IsPinned => _pinned;

	public override GCRootKind Kind => GCRootKind.LocalVar;

	public override bool IsInterior => _interior;

	public override ClrType Type => _type;

	public LocalVarRoot(ulong addr, ulong obj, ClrType type, ClrAppDomain domain, ClrThread thread, bool pinned, bool falsePos, bool interior)
	{
		Address = addr;
		Object = obj;
		_pinned = pinned;
		_falsePos = falsePos;
		_interior = interior;
		_domain = domain;
		_thread = thread;
		_type = type;
	}
}
