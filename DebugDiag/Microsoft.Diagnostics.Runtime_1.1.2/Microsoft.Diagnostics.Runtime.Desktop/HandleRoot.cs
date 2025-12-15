using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class HandleRoot : ClrRoot
{
	private static readonly Dictionary<HandleType, string> s_nameByHandleType = Enum.GetValues(typeof(HandleType)).Cast<HandleType>().ToDictionary((HandleType o) => o, (HandleType o) => $"{o} handle");

	public override ClrAppDomain AppDomain { get; }

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

	public override GCRootKind Kind { get; }

	public override string Name => s_nameByHandleType[HandleType];

	public override ClrType Type { get; }

	public HandleType HandleType { get; }

	public HandleRoot(ulong addr, ulong obj, ClrType type, HandleType handleType, GCRootKind kind, ClrAppDomain domain)
	{
		Address = addr;
		Object = obj;
		Kind = kind;
		Type = type;
		HandleType = handleType;
		AppDomain = domain;
	}
}
