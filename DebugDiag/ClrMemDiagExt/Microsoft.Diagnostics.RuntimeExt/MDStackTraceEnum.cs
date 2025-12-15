using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDStackTraceEnum : IMDStackTraceEnum
{
	private IList<ClrStackFrame> m_frames;

	private int m_curr;

	public MDStackTraceEnum(IList<ClrStackFrame> frames)
	{
		m_frames = frames;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_frames.Count;
	}

	public int Next(out ulong pIP, out ulong pSP, out string pFunction)
	{
		if (m_curr < m_frames.Count)
		{
			ClrStackFrame clrStackFrame = m_frames[m_curr];
			pIP = clrStackFrame.InstructionPointer;
			pSP = clrStackFrame.StackPointer;
			pFunction = clrStackFrame.ToString();
			return 0;
		}
		m_curr++;
		pIP = 0uL;
		pSP = 0uL;
		pFunction = null;
		if (m_curr != m_frames.Count)
		{
			return -1;
		}
		return 1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
