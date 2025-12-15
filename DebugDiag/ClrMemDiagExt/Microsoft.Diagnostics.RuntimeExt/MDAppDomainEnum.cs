using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDAppDomainEnum : IMDAppDomainEnum
{
	private IList<ClrAppDomain> m_refs;

	private int m_curr;

	public MDAppDomainEnum(IList<ClrAppDomain> refs)
	{
		m_refs = refs;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_refs.Count;
	}

	public int Next(out IMDAppDomain ppAppDomain)
	{
		if (m_curr < m_refs.Count)
		{
			ppAppDomain = new MDAppDomain(m_refs[m_curr++]);
			return 0;
		}
		if (m_curr == m_refs.Count)
		{
			m_curr++;
			ppAppDomain = null;
			return 1;
		}
		ppAppDomain = null;
		return -1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
