using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("EF0C490B-94C3-4E4D-B629-DDC134C532D8")]
public interface ICorDebugFunction2
{
	void SetJMCStatus([In] int bIsJustMyCode);

	void GetJMCStatus(out int pbIsJustMyCode);

	void EnumerateNativeCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCodeEnum ppCodeEnum);

	void GetVersionNumber(out uint pnVersion);
}
