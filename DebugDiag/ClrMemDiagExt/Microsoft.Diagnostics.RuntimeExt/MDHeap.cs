using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDHeap : IMDHeap
{
	private ClrHeap m_heap;

	public MDHeap(ClrHeap heap)
	{
		m_heap = heap;
	}

	public void GetObjectType(ulong addr, out IMDType ppType)
	{
		ppType = new MDType(m_heap.GetObjectType(addr));
	}

	public void GetExceptionObject(ulong addr, out IMDException ppExcep)
	{
		ppExcep = new MDException(m_heap.GetExceptionObject(addr));
	}

	public void EnumerateRoots(out IMDRootEnum ppEnum)
	{
		ppEnum = new MDRootEnum(new List<ClrRoot>(m_heap.EnumerateRoots()));
	}

	public void EnumerateSegments(out IMDSegmentEnum ppEnum)
	{
		ppEnum = new MDSegmentEnum(m_heap.Segments);
	}
}
