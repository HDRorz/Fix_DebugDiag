using System.Collections.Generic;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDStringEnum : IMDStringEnum
{
	private IList<string> m_data;

	private int m_curr;

	public MDStringEnum(IList<string> strings)
	{
		m_data = strings;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_data.Count;
	}

	public void Reset()
	{
		m_curr = 0;
	}

	public int Next(out string pValue)
	{
		if (m_curr < m_data.Count)
		{
			pValue = m_data[m_curr];
			m_curr++;
			return 0;
		}
		pValue = null;
		if (m_curr == m_data.Count)
		{
			m_curr++;
			return 1;
		}
		return -1;
	}
}
