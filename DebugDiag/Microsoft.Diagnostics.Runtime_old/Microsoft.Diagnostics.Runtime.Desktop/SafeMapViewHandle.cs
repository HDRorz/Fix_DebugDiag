using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal sealed class SafeMapViewHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public IntPtr BaseAddress => handle;

	private SafeMapViewHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return NativeMethods.UnmapViewOfFile(handle);
	}
}
