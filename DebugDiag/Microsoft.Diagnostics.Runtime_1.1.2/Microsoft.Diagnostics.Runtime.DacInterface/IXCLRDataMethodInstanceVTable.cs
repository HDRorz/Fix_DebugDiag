using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct IXCLRDataMethodInstanceVTable
{
	private readonly IntPtr GetTypeInstance;

	private readonly IntPtr GetDefinition;

	private readonly IntPtr GetTokenAndScope;

	private readonly IntPtr GetName;

	private readonly IntPtr GetFlags;

	private readonly IntPtr IsSameObject;

	private readonly IntPtr GetEnCVersion;

	private readonly IntPtr GetNumTypeArguments;

	private readonly IntPtr GetTypeArgumentByIndex;

	private readonly IntPtr GetILOffsetsByAddress;

	private readonly IntPtr GetAddressRangesByILOffset;

	public readonly IntPtr GetILAddressMap;

	private readonly IntPtr StartEnumExtents;

	private readonly IntPtr EnumExtent;

	private readonly IntPtr EndEnumExtents;

	private readonly IntPtr Request;

	private readonly IntPtr GetRepresentativeEntryAddress;
}
