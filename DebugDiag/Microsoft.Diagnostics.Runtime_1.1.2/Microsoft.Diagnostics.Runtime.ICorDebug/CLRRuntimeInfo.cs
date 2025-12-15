using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

internal sealed class CLRRuntimeInfo
{
	private static readonly Guid s_ClsIdClrDebuggingLegacy = new Guid("DF8395B5-A4BA-450b-A77C-A9A47762C520");

	private static Guid s_ClsIdClrProfiler = new Guid("BD097ED8-733E-43FE-8ED7-A95FF9A8448C");

	private static Guid s_CorMetaDataDispenser = new Guid("E5CB7A31-7512-11d2-89CE-0080C792E5D8");

	private readonly ICLRRuntimeInfo m_runtimeInfo;

	public CLRRuntimeInfo(object clrRuntimeInfo)
	{
		m_runtimeInfo = (ICLRRuntimeInfo)clrRuntimeInfo;
	}

	public string GetVersionString()
	{
		StringBuilder stringBuilder = new StringBuilder(26);
		int pcchBuffer = stringBuilder.Capacity;
		m_runtimeInfo.GetVersionString(stringBuilder, ref pcchBuffer);
		return stringBuilder.ToString();
	}

	public string GetRuntimeDirectory()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int pcchBuffer = 0;
		m_runtimeInfo.GetRuntimeDirectory(stringBuilder, ref pcchBuffer);
		stringBuilder.Capacity = pcchBuffer;
		int runtimeDirectory = m_runtimeInfo.GetRuntimeDirectory(stringBuilder, ref pcchBuffer);
		if (runtimeDirectory < 0)
		{
			Marshal.ThrowExceptionForHR(runtimeDirectory);
		}
		return stringBuilder.ToString();
	}

	public ICorDebug GetLegacyICorDebugInterface()
	{
		Guid riid = typeof(ICorDebug).GetGuid();
		Guid rclsid = s_ClsIdClrDebuggingLegacy;
		return (ICorDebug)m_runtimeInfo.GetInterface(ref rclsid, ref riid);
	}
}
