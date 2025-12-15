using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

internal class ProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private ProcessSafeHandle()
		: base(ownsHandle: true)
	{
	}

	private ProcessSafeHandle(IntPtr handle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		return NativeMethods.CloseHandle(handle);
	}
}
