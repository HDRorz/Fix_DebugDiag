using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDCOMInterfaceEnum : IMDCOMInterfaceEnum
{
	private IList<ComInterfaceData> m_data;

	private int m_curr;

	public MDCOMInterfaceEnum(IList<ComInterfaceData> data)
	{
		m_data = data;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_data.Count;
	}

	public void Reset()
	{
		m_curr = 0;
	}

	public int Next(IMDType pType, out ulong pInterfacePtr)
	{
		if (m_curr < m_data.Count)
		{
			pType = new MDType(m_data[m_curr].Type);
			pInterfacePtr = m_data[m_curr].InterfacePointer;
			m_curr++;
			return 0;
		}
		pType = null;
		pInterfacePtr = 0uL;
		if (m_curr == m_data.Count)
		{
			m_curr++;
			return 1;
		}
		return -1;
	}
}
