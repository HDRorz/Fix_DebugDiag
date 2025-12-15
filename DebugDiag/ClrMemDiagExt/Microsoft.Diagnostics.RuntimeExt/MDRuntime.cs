using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDRuntime : IMDRuntime
{
	private ClrRuntime m_runtime;

	public MDRuntime(ClrRuntime runtime)
	{
		m_runtime = runtime;
	}

	public void IsServerGC(out int pServerGC)
	{
		pServerGC = (m_runtime.ServerGC ? 1 : 0);
	}

	public void GetHeapCount(out int pHeapCount)
	{
		pHeapCount = m_runtime.HeapCount;
	}

	public int ReadVirtual(ulong addr, byte[] buffer, int requested, out int pRead)
	{
		int bytesRead;
		bool num = m_runtime.ReadMemory(addr, buffer, requested, out bytesRead);
		pRead = bytesRead;
		if (!num)
		{
			return -1;
		}
		return 0;
	}

	public int ReadPtr(ulong addr, out ulong pValue)
	{
		if (!m_runtime.ReadPointer(addr, out pValue))
		{
			return 0;
		}
		return 1;
	}

	public void Flush()
	{
		m_runtime.Flush();
	}

	public void GetHeap(out IMDHeap ppHeap)
	{
		ppHeap = new MDHeap(m_runtime.GetHeap());
	}

	public void EnumerateAppDomains(out IMDAppDomainEnum ppEnum)
	{
		ppEnum = new MDAppDomainEnum(m_runtime.AppDomains);
	}

	public void EnumerateThreads(out IMDThreadEnum ppEnum)
	{
		ppEnum = new MDThreadEnum(m_runtime.Threads);
	}

	public void EnumerateFinalizerQueue(out IMDObjectEnum ppEnum)
	{
		ppEnum = new MDObjectEnum(new List<ulong>(m_runtime.EnumerateFinalizerQueue()));
	}

	public void EnumerateGCHandles(out IMDHandleEnum ppEnum)
	{
		ppEnum = new MDHandleEnum(m_runtime.EnumerateHandles());
	}

	public void EnumerateMemoryRegions(out IMDMemoryRegionEnum ppEnum)
	{
		ppEnum = new MDMemoryRegionEnum(new List<ClrMemoryRegion>(m_runtime.EnumerateMemoryRegions()));
	}
}
