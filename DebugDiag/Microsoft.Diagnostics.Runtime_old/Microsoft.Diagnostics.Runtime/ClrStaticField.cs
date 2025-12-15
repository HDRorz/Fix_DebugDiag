using System;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrStaticField : ClrField
{
	public virtual bool HasDefaultValue => false;

	[Obsolete("Use GetValue instead.")]
	public virtual object GetFieldValue(ClrAppDomain domain)
	{
		return GetValue(domain);
	}

	[Obsolete("Use GetAddress instead.")]
	public virtual ulong GetFieldAddress(ClrAppDomain domain)
	{
		return GetAddress(domain);
	}

	public abstract bool IsInitialized(ClrAppDomain appDomain);

	public abstract object GetValue(ClrAppDomain appDomain);

	public abstract ulong GetAddress(ClrAppDomain appDomain);

	public virtual object GetDefaultValue()
	{
		throw new NotImplementedException();
	}
}
