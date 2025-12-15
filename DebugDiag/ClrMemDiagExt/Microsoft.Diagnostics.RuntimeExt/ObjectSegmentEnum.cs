using System;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class ObjectSegmentEnum : IMDObjectEnum
{
	private ClrSegment m_seg;

	private ulong m_obj;

	private bool m_done;

	public ObjectSegmentEnum(ClrSegment seg)
	{
		m_seg = seg;
		m_obj = seg.FirstObject;
	}

	public int Next(int count, ulong[] refs, out int pWrote)
	{
		if (m_obj == 0L && !m_done)
		{
			m_done = true;
			pWrote = 0;
			return 1;
		}
		if (m_done)
		{
			pWrote = 0;
			return -1;
		}
		int num = 0;
		while (num < count && m_obj != 0L)
		{
			refs[num++] = m_obj;
			m_obj = m_seg.NextObject(m_obj);
		}
		pWrote = num;
		if (num < count || m_obj == 0L)
		{
			m_done = true;
			return 1;
		}
		return 0;
	}

	public void GetCount(out int pCount)
	{
		throw new NotImplementedException();
	}

	public void Reset()
	{
		m_obj = 0uL;
		m_done = true;
	}
}
