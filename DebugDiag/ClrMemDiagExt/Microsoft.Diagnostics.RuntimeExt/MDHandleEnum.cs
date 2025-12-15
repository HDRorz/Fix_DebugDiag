using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDHandleEnum : IMDHandleEnum
{
	private IList<ClrHandle> m_handles;

	private int m_curr;

	public MDHandleEnum(IEnumerable<ClrHandle> handles)
	{
		m_handles = new List<ClrHandle>(handles);
	}

	public void GetCount(out int pCount)
	{
		throw new NotImplementedException();
	}

	public int Next(out IMDHandle ppHandle)
	{
		if (m_curr < m_handles.Count)
		{
			ppHandle = new MDHandle(m_handles[m_curr++]);
			return 0;
		}
		if (m_curr == m_handles.Count)
		{
			m_curr++;
			ppHandle = null;
			return 1;
		}
		ppHandle = null;
		return -1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
