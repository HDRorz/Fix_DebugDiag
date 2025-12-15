using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime;

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
		return FreeLibrary(handle);
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool FreeLibrary(IntPtr hModule);
}
