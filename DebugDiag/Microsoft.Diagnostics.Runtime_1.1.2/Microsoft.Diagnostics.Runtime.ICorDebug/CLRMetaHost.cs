using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

internal sealed class CLRMetaHost
{
	private readonly ICLRMetaHost m_metaHost;

	public const int MaxVersionStringLength = 26;

	private static readonly Guid clsidCLRMetaHost = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");

	public CLRMetaHost()
	{
		Guid riid = typeof(ICLRMetaHost).GetGuid();
		Guid clsid = clsidCLRMetaHost;
		NativeMethods.CLRCreateInstance(ref clsid, ref riid, out var metahostInterface);
		m_metaHost = (ICLRMetaHost)metahostInterface;
	}

	public CLRRuntimeInfo GetInstalledRuntimeByVersion(string version)
	{
		foreach (CLRRuntimeInfo item in EnumerateInstalledRuntimes())
		{
			if (item.GetVersionString().ToLower() == version.ToLower())
			{
				return item;
			}
		}
		return null;
	}

	public CLRRuntimeInfo GetLoadedRuntimeByVersion(int processId, string version)
	{
		foreach (CLRRuntimeInfo item in EnumerateLoadedRuntimes(processId))
		{
			if (item.GetVersionString().Equals(version, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	public IEnumerable<CLRRuntimeInfo> EnumerateInstalledRuntimes()
	{
		List<CLRRuntimeInfo> list = new List<CLRRuntimeInfo>();
		IEnumUnknown enumUnknown = m_metaHost.EnumerateInstalledRuntimes();
		object rgelt;
		while (enumUnknown.Next(1, out rgelt, IntPtr.Zero) == 0)
		{
			list.Add(new CLRRuntimeInfo(rgelt));
		}
		return list;
	}

	public IEnumerable<CLRRuntimeInfo> EnumerateLoadedRuntimes(int processId)
	{
		List<CLRRuntimeInfo> list = new List<CLRRuntimeInfo>();
		IEnumUnknown enumUnknown;
		using (ProcessSafeHandle processSafeHandle = NativeMethods.OpenProcess(2097151, bInheritHandle: false, processId))
		{
			if (processSafeHandle.IsInvalid)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			enumUnknown = m_metaHost.EnumerateLoadedRuntimes(processSafeHandle);
		}
		object rgelt;
		while (enumUnknown.Next(1, out rgelt, IntPtr.Zero) == 0)
		{
			list.Add(new CLRRuntimeInfo(rgelt));
		}
		return list;
	}

	public CLRRuntimeInfo GetRuntime(string version)
	{
		Guid riid = typeof(ICLRRuntimeInfo).GetGuid();
		return new CLRRuntimeInfo(m_metaHost.GetRuntime(version, ref riid));
	}
}
