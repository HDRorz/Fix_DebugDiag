using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("AD1B3588-0EF0-4744-A496-AA09A9F80371")]
[InterfaceType(1)]
[ComConversionLoss]
public interface ICorDebugProcess2
{
	void GetThreadForTaskID([In] ulong taskid, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread2 ppThread);

	void GetVersion(out _COR_VERSION version);

	void SetUnmanagedBreakpoint([In] ulong address, [In] uint bufsize, [Out][MarshalAs(UnmanagedType.LPArray)] byte[] buffer, out uint bufLen);

	void ClearUnmanagedBreakpoint([In] ulong address);

	void SetDesiredNGENCompilerFlags([In] uint pdwFlags);

	void GetDesiredNGENCompilerFlags(out uint pdwFlags);

	void GetReferenceValueFromGCHandle([In] IntPtr handle, [MarshalAs(UnmanagedType.Interface)] out ICorDebugReferenceValue pOutValue);
}
