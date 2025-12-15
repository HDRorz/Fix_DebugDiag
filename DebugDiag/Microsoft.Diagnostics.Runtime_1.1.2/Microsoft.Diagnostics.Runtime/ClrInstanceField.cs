namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrInstanceField : ClrField
{
	public virtual object GetValue(ulong objRef)
	{
		return GetValue(objRef, interior: false, convertStrings: true);
	}

	public virtual object GetValue(ulong objRef, bool interior)
	{
		return GetValue(objRef, interior, convertStrings: true);
	}

	public abstract object GetValue(ulong objRef, bool interior, bool convertStrings);

	public virtual ulong GetAddress(ulong objRef)
	{
		return GetAddress(objRef, interior: false);
	}

	public abstract ulong GetAddress(ulong objRef, bool interior);
}
