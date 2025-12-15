namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeStaticVar : ClrRoot
{
	private string _name;

	private bool _pinned;

	private bool _interior;

	private ClrType _type;

	private ClrAppDomain _appDomain;

	public override GCRootKind Kind => GCRootKind.StaticVar;

	public override ClrType Type => _type;

	public override string Name => _name;

	public override bool IsPinned => _pinned;

	public override bool IsInterior => _interior;

	public override ClrAppDomain AppDomain => _appDomain;

	public NativeStaticVar(NativeRuntime runtime, ulong addr, ulong obj, ClrType type, string name, bool pinned, bool interior)
	{
		Address = addr;
		Object = obj;
		_type = type;
		_name = name;
		_pinned = pinned;
		_interior = interior;
		_type = runtime.GetHeap().GetObjectType(obj);
		_appDomain = runtime.GetRhAppDomain();
	}
}
