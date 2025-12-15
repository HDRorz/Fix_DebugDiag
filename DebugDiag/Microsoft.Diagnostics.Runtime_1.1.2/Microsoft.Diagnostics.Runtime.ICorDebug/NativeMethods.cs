using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

internal static class NativeMethods
{
	public enum ProcessAccessOptions
	{
		ProcessTerminate = 1,
		ProcessCreateThread = 2,
		ProcessSetSessionID = 4,
		ProcessVMOperation = 8,
		ProcessVMRead = 0x10,
		ProcessVMWrite = 0x20,
		ProcessDupHandle = 0x40,
		ProcessCreateProcess = 0x80,
		ProcessSetQuota = 0x100,
		ProcessSetInformation = 0x200,
		ProcessQueryInformation = 0x400,
		ProcessSuspendResume = 0x800,
		Synchronize = 0x100000
	}

	private const string Kernel32LibraryName = "kernel32.dll";

	private const string Ole32LibraryName = "ole32.dll";

	private const string ShlwapiLibraryName = "shlwapi.dll";

	private const string ShimLibraryName = "mscoree.dll";

	public const int MAX_PATH = 260;

	[DllImport("kernel32.dll")]
	public static extern bool CloseHandle(IntPtr handle);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern ICorDebug CreateDebuggingInterfaceFromVersion(int iDebuggerVersion, string szDebuggeeVersion);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern void GetVersionFromProcess(ProcessSafeHandle hProcess, StringBuilder versionString, int bufferSize, out int dwLength);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern void GetRequestedRuntimeVersion(string pExe, StringBuilder pVersion, int cchBuffer, out int dwLength);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern void CLRCreateInstance(ref Guid clsid, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object metahostInterface);

	[DllImport("kernel32.dll")]
	public static extern ProcessSafeHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
}
