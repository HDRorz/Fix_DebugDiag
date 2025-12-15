namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrThreadStaticField : ClrField
{
	public virtual object GetValue(ClrAppDomain appDomain, ClrThread thread)
	{
		return GetValue(appDomain, thread, convertStrings: true);
	}

	public abstract object GetValue(ClrAppDomain appDomain, ClrThread thread, bool convertStrings);

	public abstract ulong GetAddress(ClrAppDomain appDomain, ClrThread thread);
}
