using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDMemoryRegion : IMDMemoryRegion
{
	private ClrMemoryRegion m_region;

	public MDMemoryRegion(ClrMemoryRegion region)
	{
		m_region = region;
	}

	public void GetRegionInfo(out ulong pAddress, out ulong pSize, out MDMemoryRegionType pType)
	{
		pAddress = m_region.Address;
		pSize = m_region.Size;
		pType = (MDMemoryRegionType)m_region.Type;
	}

	public void GetAppDomain(out IMDAppDomain ppDomain)
	{
		if (m_region.AppDomain != null)
		{
			ppDomain = new MDAppDomain(m_region.AppDomain);
		}
		else
		{
			ppDomain = null;
		}
	}

	public void GetModule(out string pModule)
	{
		pModule = m_region.Module;
	}

	public void GetHeapNumber(out int pHeap)
	{
		pHeap = m_region.HeapNumber;
	}

	public void GetDisplayString(out string pName)
	{
		pName = m_region.ToString(detailed: true);
	}

	public void GetSegmentType(out MDSegmentType pType)
	{
		pType = (MDSegmentType)m_region.GCSegmentType;
	}
}
