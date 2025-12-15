using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDThreadEnum : IMDThreadEnum
{
	private IList<ClrThread> m_threads;

	private int m_curr;

	public MDThreadEnum(IList<ClrThread> threads)
	{
		m_threads = threads;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_threads.Count;
	}

	public int Next(out IMDThread ppThread)
	{
		if (m_curr < m_threads.Count)
		{
			ppThread = new MDThread(m_threads[m_curr++]);
			return 0;
		}
		if (m_curr == m_threads.Count)
		{
			m_curr++;
			ppThread = null;
			return 1;
		}
		ppThread = null;
		return -1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
