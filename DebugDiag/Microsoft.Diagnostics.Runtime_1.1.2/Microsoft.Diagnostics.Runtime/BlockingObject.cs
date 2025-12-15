using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

[Obsolete]
public abstract class BlockingObject
{
	public abstract ulong Object { get; }

	public abstract bool Taken { get; }

	public abstract int RecursionCount { get; }

	public abstract ClrThread Owner { get; }

	public abstract bool HasSingleOwner { get; }

	public abstract IList<ClrThread> Owners { get; }

	public abstract IList<ClrThread> Waiters { get; }

	public abstract BlockingReason Reason { get; internal set; }
}
