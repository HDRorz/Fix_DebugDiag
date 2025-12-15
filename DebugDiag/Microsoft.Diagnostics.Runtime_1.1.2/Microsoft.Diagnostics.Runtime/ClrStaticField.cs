using System;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrStaticField : ClrField
{
	public virtual bool HasDefaultValue => false;

	public abstract bool IsInitialized(ClrAppDomain appDomain);

	public virtual object GetValue(ClrAppDomain appDomain)
	{
		return GetValue(appDomain, convertStrings: true);
	}

	public abstract object GetValue(ClrAppDomain appDomain, bool convertStrings);

	public abstract ulong GetAddress(ClrAppDomain appDomain);

	public virtual object GetDefaultValue()
	{
		throw new NotImplementedException();
	}
}
