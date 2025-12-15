using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct ISOSStackRefEnumVTable
{
	private readonly IntPtr Skip;

	private readonly IntPtr Reset;

	private readonly IntPtr GetCount;

	public readonly IntPtr Next;
}
