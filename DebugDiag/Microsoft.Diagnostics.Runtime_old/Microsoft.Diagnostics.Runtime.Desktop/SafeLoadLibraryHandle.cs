using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal sealed class SafeLoadLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public IntPtr BaseAddress => handle;

	private SafeLoadLibraryHandle()
		: base(ownsHandle: true)
	{
	}

	public SafeLoadLibraryHandle(IntPtr handle)
		: base(ownsHandle: true)
	{
		SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		return NativeMethods.FreeLibrary(handle);
	}
}
