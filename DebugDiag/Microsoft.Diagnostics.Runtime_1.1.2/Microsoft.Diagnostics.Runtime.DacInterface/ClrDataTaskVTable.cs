using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct ClrDataTaskVTable
{
	private readonly IntPtr GetProcess;

	private readonly IntPtr GetCurrentAppDomain;

	private readonly IntPtr GetUniqueID;

	private readonly IntPtr GetFlags;

	private readonly IntPtr IsSameObject;

	private readonly IntPtr GetManagedObject;

	private readonly IntPtr GetDesiredExecutionState;

	private readonly IntPtr SetDesiredExecutionState;

	public readonly IntPtr CreateStackWalk;

	private readonly IntPtr GetOSThreadID;

	private readonly IntPtr GetContext;

	private readonly IntPtr SetContext;

	private readonly IntPtr GetCurrentExceptionState;

	private readonly IntPtr Request;

	private readonly IntPtr GetName;

	private readonly IntPtr GetLastExceptionState;
}
