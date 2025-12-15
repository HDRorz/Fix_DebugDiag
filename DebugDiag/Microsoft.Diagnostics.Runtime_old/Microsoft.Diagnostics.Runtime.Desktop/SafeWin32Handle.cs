using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal sealed class SafeWin32Handle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeWin32Handle()
		: base(ownsHandle: true)
	{
	}

	public SafeWin32Handle(IntPtr handle)
		: this(handle, ownsHandle: true)
	{
	}

	public SafeWin32Handle(IntPtr handle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		return NativeMethods.CloseHandle(handle);
	}
}
