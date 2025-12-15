using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDSegmentEnum : IMDSegmentEnum
{
	private IList<ClrSegment> m_segments;

	private int m_curr;

	public MDSegmentEnum(IList<ClrSegment> segments)
	{
		m_segments = segments;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_segments.Count;
	}

	public int Next(out IMDSegment ppSegment)
	{
		if (m_curr < m_segments.Count)
		{
			ppSegment = new MDSegment(m_segments[m_curr++]);
			return 0;
		}
		if (m_curr == m_segments.Count)
		{
			m_curr++;
			ppSegment = null;
			return 1;
		}
		ppSegment = null;
		return -1;
	}

	public void Reset()
	{
		m_curr = 0;
	}
}
