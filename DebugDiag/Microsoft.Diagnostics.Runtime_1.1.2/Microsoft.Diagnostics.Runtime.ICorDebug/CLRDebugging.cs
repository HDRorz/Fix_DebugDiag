using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

internal sealed class CLRDebugging
{
	private static readonly Guid clsidCLRDebugging = new Guid("BACC578D-FBDD-48a4-969F-02D932B74634");

	private readonly ICLRDebugging _clrDebugging;

	public CLRDebugging()
	{
		Guid riid = typeof(ICLRDebugging).GetGuid();
		Guid clsid = clsidCLRDebugging;
		NativeMethods.CLRCreateInstance(ref clsid, ref riid, out var metahostInterface);
		_clrDebugging = (ICLRDebugging)metahostInterface;
	}

	public static ICorDebug GetDebuggerForProcess(int processID, string minimumVersion, DebuggerCallBacks callBacks = null)
	{
		CLRMetaHost cLRMetaHost = new CLRMetaHost();
		CLRRuntimeInfo cLRRuntimeInfo = null;
		foreach (CLRRuntimeInfo item in cLRMetaHost.EnumerateLoadedRuntimes(processID))
		{
			if (cLRRuntimeInfo == null || string.Compare(cLRRuntimeInfo.GetVersionString(), item.GetVersionString(), StringComparison.OrdinalIgnoreCase) < 0)
			{
				cLRRuntimeInfo = item;
			}
		}
		if (cLRRuntimeInfo == null)
		{
			throw new Exception("Could not enumerate .NET runtimes on the system.");
		}
		string versionString = cLRRuntimeInfo.GetVersionString();
		if (string.Compare(versionString, minimumVersion, StringComparison.OrdinalIgnoreCase) < 0)
		{
			throw new Exception("Runtime in process " + versionString + " below the minimum of " + minimumVersion);
		}
		ICorDebug obj = cLRRuntimeInfo.GetLegacyICorDebugInterface() ?? throw new ArgumentNullException("rawDebuggingAPI");
		obj.Initialize();
		if (callBacks == null)
		{
			callBacks = new DebuggerCallBacks();
		}
		obj.SetManagedHandler(callBacks);
		return obj;
	}

	public static ICorDebugProcess CreateICorDebugProcess(ulong baseAddress, ICorDebugDataTarget dataTarget, ICLRDebuggingLibraryProvider libraryProvider)
	{
		Version version;
		ClrDebuggingProcessFlags flags;
		ICorDebugProcess process;
		int num = new CLRDebugging().TryOpenVirtualProcess(baseAddress, dataTarget, libraryProvider, new Version(4, 6, 32767, 32767), out version, out flags, out process);
		if (num < 0)
		{
			if (num != -2146231228 && num != -2146231226 && num != -2146231225)
			{
				Marshal.ThrowExceptionForHR(num);
			}
			return null;
		}
		return process;
	}

	public ICorDebugProcess OpenVirtualProcess(ulong moduleBaseAddress, ICorDebugDataTarget dataTarget, ICLRDebuggingLibraryProvider libraryProvider, Version maxDebuggerSupportedVersion, out Version version, out ClrDebuggingProcessFlags flags)
	{
		ICorDebugProcess process;
		int num = TryOpenVirtualProcess(moduleBaseAddress, dataTarget, libraryProvider, maxDebuggerSupportedVersion, out version, out flags, out process);
		if (num < 0)
		{
			throw new Exception("Failed to OpenVirtualProcess for module at " + moduleBaseAddress + ".  hr = " + num.ToString("x"));
		}
		return process;
	}

	public int TryOpenVirtualProcess(ulong moduleBaseAddress, ICorDebugDataTarget dataTarget, ICLRDebuggingLibraryProvider libraryProvider, Version maxDebuggerSupportedVersion, out Version version, out ClrDebuggingProcessFlags flags, out ICorDebugProcess process)
	{
		ClrDebuggingVersion maxDebuggerSupportedVersion2 = default(ClrDebuggingVersion);
		ClrDebuggingVersion version2 = default(ClrDebuggingVersion);
		maxDebuggerSupportedVersion2.StructVersion = 0;
		maxDebuggerSupportedVersion2.Major = (short)maxDebuggerSupportedVersion.Major;
		maxDebuggerSupportedVersion2.Minor = (short)maxDebuggerSupportedVersion.Minor;
		maxDebuggerSupportedVersion2.Build = (short)maxDebuggerSupportedVersion.Build;
		maxDebuggerSupportedVersion2.Revision = (short)maxDebuggerSupportedVersion.Revision;
		object process2 = null;
		version2.StructVersion = 0;
		Guid riidProcess = typeof(ICorDebugProcess).GetGuid();
		int num = _clrDebugging.OpenVirtualProcess(moduleBaseAddress, dataTarget, libraryProvider, ref maxDebuggerSupportedVersion2, ref riidProcess, out process2, ref version2, out flags);
		version = new Version(version2.Major, version2.Minor, version2.Build, version2.Revision);
		if (num < 0)
		{
			process = null;
			return num;
		}
		process = (ICorDebugProcess)process2;
		return 0;
	}

	public bool CanUnloadNow(IntPtr moduleHandle)
	{
		int num = _clrDebugging.CanUnloadNow(moduleHandle);
		switch (num)
		{
		case 0:
			return true;
		case 1:
			return false;
		default:
			Marshal.ThrowExceptionForHR(num);
			throw new Exception();
		}
	}
}
