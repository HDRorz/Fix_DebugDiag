namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeHandleRoot : ClrRoot
{
	private string _name;

	private ClrType _type;

	private ClrAppDomain _appDomain;

	private GCRootKind _kind;

	public override GCRootKind Kind => _kind;

	public override ClrType Type => _type;

	public override string Name => _name;

	public override ClrAppDomain AppDomain => _appDomain;

	public NativeHandleRoot(ulong addr, ulong obj, ulong dependentTarget, ClrType type, int hndType, ClrAppDomain domain, string name)
	{
		Init(addr, obj, dependentTarget, type, hndType, domain, name);
	}

	public NativeHandleRoot(ulong addr, ulong obj, ClrType type, int hndType, ClrAppDomain domain, string name)
	{
		Init(addr, obj, 0uL, type, hndType, domain, name);
	}

	private void Init(ulong addr, ulong obj, ulong dependentTarget, ClrType type, int hndType, ClrAppDomain domain, string name)
	{
		switch (hndType)
		{
		case 7:
			_kind = GCRootKind.AsyncPinning;
			break;
		case 3:
			_kind = GCRootKind.Pinning;
			break;
		case 0:
		case 1:
			_kind = GCRootKind.Weak;
			break;
		default:
			_kind = GCRootKind.Strong;
			break;
		}
		Address = addr;
		_name = name;
		_type = type;
		_appDomain = domain;
		if (hndType == 6 && dependentTarget != 0L)
		{
			Object = dependentTarget;
		}
		else
		{
			Object = obj;
		}
	}
}
