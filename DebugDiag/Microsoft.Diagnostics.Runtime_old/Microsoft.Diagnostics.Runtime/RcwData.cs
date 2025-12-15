using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class RcwData
{
	public abstract ulong IUnknown { get; }

	public abstract ulong VTablePointer { get; }

	public abstract int RefCount { get; }

	public abstract ulong Object { get; }

	public abstract bool Disconnected { get; }

	public abstract uint CreatorThread { get; }

	public abstract ulong WinRTObject { get; }

	public abstract IList<ComInterfaceData> Interfaces { get; }
}
