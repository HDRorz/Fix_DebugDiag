using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class HandleRoot : ClrRoot
{
	private GCRootKind _kind;

	private string _name;

	private ClrType _type;

	private ClrAppDomain _domain;

	public override ClrAppDomain AppDomain => _domain;

	public override bool IsPinned
	{
		get
		{
			if (Kind != GCRootKind.Pinning)
			{
				return Kind == GCRootKind.AsyncPinning;
			}
			return true;
		}
	}

	public override GCRootKind Kind => _kind;

	public override string Name => _name;

	public override ClrType Type => _type;

	public HandleRoot(ulong addr, ulong obj, ClrType type, HandleType hndType, GCRootKind kind, ClrAppDomain domain)
	{
		_name = Enum.GetName(typeof(HandleType), hndType) + " handle";
		Address = addr;
		Object = obj;
		_kind = kind;
		_type = type;
		_domain = domain;
	}
}
