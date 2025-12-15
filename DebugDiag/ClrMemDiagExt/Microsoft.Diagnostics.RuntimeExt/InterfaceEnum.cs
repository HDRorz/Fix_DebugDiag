using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class InterfaceEnum : IMDInterfaceEnum
{
	private IList<ClrInterface> m_data;

	private int m_curr;

	public InterfaceEnum(IList<ClrInterface> interfaces)
	{
		m_data = interfaces;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_data.Count;
	}

	public void Reset()
	{
		m_curr = 0;
	}

	public int Next(out IMDInterface pValue)
	{
		if (m_curr < m_data.Count)
		{
			pValue = new MDInterface(m_data[m_curr]);
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
