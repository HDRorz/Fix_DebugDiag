using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct ISOSHandleEnumVTable
{
	private readonly IntPtr Skip;

	private readonly IntPtr Reset;

	private readonly IntPtr GetCount;

	public readonly IntPtr Next;
}
