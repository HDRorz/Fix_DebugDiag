using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDTarget : IMDTarget
{
	private DataTarget m_target;

	public MDTarget(string crashdump)
	{
		m_target = DataTarget.LoadCrashDump(crashdump);
	}

	public MDTarget(object iDebugClient)
	{
		m_target = DataTarget.CreateFromDebuggerInterface((IDebugClient)iDebugClient);
	}

	public void GetRuntimeCount(out int pCount)
	{
		pCount = m_target.ClrVersions.Count;
	}

	public void GetRuntimeInfo(int num, out IMDRuntimeInfo ppInfo)
	{
		ppInfo = new MDRuntimeInfo(m_target.ClrVersions[num]);
	}

	public void GetPointerSize(out int pPointerSize)
	{
		pPointerSize = (int)m_target.PointerSize;
	}

	public void CreateRuntimeFromDac(string dacLocation, out IMDRuntime ppRuntime)
	{
		var clrInfo = m_target.ClrVersions[0]; // Use first available CLR version
		ppRuntime = new MDRuntime(clrInfo.CreateRuntime(dacLocation));
	}

	public void CreateRuntimeFromIXCLR(object ixCLRProcess, out IMDRuntime ppRuntime)
	{
		var clrInfo = m_target.ClrVersions[0]; // Use first available CLR version
		ppRuntime = new MDRuntime(clrInfo.CreateRuntime(ixCLRProcess));
	}
}
