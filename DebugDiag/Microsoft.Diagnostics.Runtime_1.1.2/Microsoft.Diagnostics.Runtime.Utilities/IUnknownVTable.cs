using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public struct IUnknownVTable
{
	public IntPtr QueryInterface;

	public IntPtr AddRef;

	public IntPtr Release;
}
