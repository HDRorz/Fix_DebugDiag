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
		ppRuntime = new MDRuntime(m_target.CreateRuntime(dacLocation));
	}

	public void CreateRuntimeFromIXCLR(object ixCLRProcess, out IMDRuntime ppRuntime)
	{
		ppRuntime = new MDRuntime(m_target.CreateRuntime(ixCLRProcess));
	}
}
