using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDMemoryRegionEnum : IMDMemoryRegionEnum
{
	private IList<ClrMemoryRegion> m_regions;

	private int m_curr;

	public MDMemoryRegionEnum(IList<ClrMemoryRegion> regions)
	{
		m_regions = regions;
	}

	public void GetCount(out int pCount)
	{
		pCount = m_regions.Count;
	}

	public int Next(out IMDMemoryRegion ppRegion)
	{
		if (m_curr < m_regions.Count)
		{
			ppRegion = new MDMemoryRegion(m_regions[m_curr++]);
			return 0;
		}
		if (m_curr == m_regions.Count)
		{
			m_curr++;
			ppRegion = null;
			return 1;
		}
		ppRegion = null;
		return -1;
	}

	public void Reset()
	{
		throw new NotImplementedException();
	}
}
