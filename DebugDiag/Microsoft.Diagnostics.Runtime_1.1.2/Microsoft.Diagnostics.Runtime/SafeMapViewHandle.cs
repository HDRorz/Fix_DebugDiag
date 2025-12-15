using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime;

internal sealed class SafeMapViewHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public IntPtr BaseAddress => handle;

	private SafeMapViewHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return UnmapViewOfFile(handle);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnmapViewOfFile(IntPtr baseAddress);
}
