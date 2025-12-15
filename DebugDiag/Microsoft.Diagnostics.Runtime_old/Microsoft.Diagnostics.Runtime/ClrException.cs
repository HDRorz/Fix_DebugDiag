using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrException
{
	public abstract ClrType Type { get; }

	public abstract string Message { get; }

	public abstract ulong Address { get; }

	public abstract ClrException Inner { get; }

	public abstract int HResult { get; }

	public abstract IList<ClrStackFrame> StackTrace { get; }
}
