using System.Collections.Generic;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDObjectEnum : IMDObjectEnum
{
	private IList<ulong> m_refs;

	private int m_curr;

	public MDObjectEnum(IList<ulong> refs)
	{
		m_refs = refs;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_refs.Count;
	}

	public int Next(int count, ulong[] refs, out int pWrote)
	{
		if (m_curr == m_refs.Count)
		{
			m_curr = m_refs.Count + 1;
			pWrote = 0;
			return 1;
		}
		if (m_curr > m_refs.Count)
		{
			pWrote = 0;
			return -1;
		}
		int num = 0;
		while (m_curr < m_refs.Count && num < count)
		{
			refs[num] = m_refs[m_curr];
			num++;
			m_curr++;
		}
		pWrote = num;
		if (num != count)
		{
			m_curr = m_refs.Count + 1;
			return 1;
		}
		return 0;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
