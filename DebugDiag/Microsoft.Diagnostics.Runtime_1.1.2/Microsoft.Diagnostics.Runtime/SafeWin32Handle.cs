using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime;

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
		return CloseHandle(handle);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(IntPtr handle);
}
