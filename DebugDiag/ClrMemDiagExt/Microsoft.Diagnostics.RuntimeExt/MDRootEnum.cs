using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDRootEnum : IMDRootEnum
{
	private IList<ClrRoot> m_roots;

	private int m_curr;

	public MDRootEnum(IList<ClrRoot> roots)
	{
		m_roots = roots;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_curr;
	}

	public int Next(out IMDRoot ppRoot)
	{
		if (m_curr < m_roots.Count)
		{
			ppRoot = new MDRoot(m_roots[m_curr]);
			return 0;
		}
		if (m_curr == m_roots.Count)
		{
			ppRoot = null;
			return 1;
		}
		ppRoot = null;
		return -1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
