using System;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrInstanceField : ClrField
{
	[Obsolete("Use GetValue instead.")]
	public virtual object GetFieldValue(ulong objRef)
	{
		return GetValue(objRef, interior: false);
	}

	[Obsolete("Use GetValue instead.")]
	public virtual object GetFieldValue(ulong objRef, bool interior)
	{
		return GetValue(objRef, interior);
	}

	[Obsolete("Use GetAddress instead.")]
	public virtual ulong GetFieldAddress(ulong objRef)
	{
		return GetAddress(objRef, interior: false);
	}

	[Obsolete("Use GetAddress instead.")]
	public virtual ulong GetFieldAddress(ulong objRef, bool interior)
	{
		return GetAddress(objRef, interior: false);
	}

	public virtual object GetValue(ulong objRef)
	{
		return GetValue(objRef, interior: false);
	}

	public abstract object GetValue(ulong objRef, bool interior);

	public virtual ulong GetAddress(ulong objRef)
	{
		return GetAddress(objRef, interior: false);
	}

	public abstract ulong GetAddress(ulong objRef, bool interior);
}
