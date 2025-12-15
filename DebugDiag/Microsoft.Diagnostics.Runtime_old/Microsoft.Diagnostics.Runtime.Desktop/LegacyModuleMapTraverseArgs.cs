using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyModuleMapTraverseArgs
{
	private uint _setToZero;

	public ulong module;

	public IntPtr pCallback;

	public IntPtr token;
}
