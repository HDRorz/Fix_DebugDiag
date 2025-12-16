using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDRuntimeInfo : IMDRuntimeInfo
{
	private ClrInfo m_info;

	public MDRuntimeInfo(ClrInfo info)
	{
		m_info = info;
	}

	public void GetRuntimeVersion(out string pVersion)
	{
		pVersion = m_info.ToString();
	}

	public void GetDacLocation(out string pLocation)
	{
		pLocation = m_info.LocalMatchingDac;
	}

	public void GetDacRequestData(out int pTimestamp, out int pFilesize)
	{
		pTimestamp = (int)m_info.DacInfo.TimeStamp;
		pFilesize = (int)m_info.DacInfo.FileSize;
	}

	public void GetDacRequestFilename(out string pRequestFileName)
	{
		pRequestFileName = m_info.DacInfo.FileName;
	}
}
