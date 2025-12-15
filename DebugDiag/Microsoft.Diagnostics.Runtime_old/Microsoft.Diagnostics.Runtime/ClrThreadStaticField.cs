using System;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrThreadStaticField : ClrField
{
	[Obsolete("Use GetValue instead.")]
	public virtual object GetFieldValue(ClrAppDomain domain, ClrThread thread)
	{
		return GetValue(domain, thread);
	}

	[Obsolete("Use GetAddress instead.")]
	public virtual ulong GetFieldAddress(ClrAppDomain domain, ClrThread thread)
	{
		return GetAddress(domain, thread);
	}

	public abstract object GetValue(ClrAppDomain appDomain, ClrThread thread);

	public abstract ulong GetAddress(ClrAppDomain appDomain, ClrThread thread);
}
