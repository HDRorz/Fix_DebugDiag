using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyModuleMapTraverseArgs
{
	private readonly uint _setToZero;

	public ulong Module;

	public IntPtr Callback;

	public IntPtr Token;
}
