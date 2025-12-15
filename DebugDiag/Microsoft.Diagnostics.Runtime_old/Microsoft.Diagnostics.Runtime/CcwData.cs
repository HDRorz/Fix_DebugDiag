using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class CcwData
{
	public abstract ulong IUnknown { get; }

	public abstract ulong Object { get; }

	public abstract ulong Handle { get; }

	public abstract int RefCount { get; }

	public abstract IList<ComInterfaceData> Interfaces { get; }
}
