using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDSegment : IMDSegment
{
	private ClrSegment m_seg;

	public MDSegment(ClrSegment seg)
	{
		m_seg = seg;
	}

	public void GetStart(out ulong pAddress)
	{
		pAddress = m_seg.Start;
	}

	public void GetEnd(out ulong pAddress)
	{
		pAddress = m_seg.End;
	}

	public void GetReserveLimit(out ulong pAddress)
	{
		pAddress = m_seg.ReservedEnd;
	}

	public void GetCommitLimit(out ulong pAddress)
	{
		pAddress = m_seg.CommittedEnd;
	}

	public void GetLength(out ulong pLength)
	{
		pLength = m_seg.Length;
	}

	public void GetProcessorAffinity(out int pProcessor)
	{
		pProcessor = m_seg.ProcessorAffinity;
	}

	public void IsLarge(out int pLarge)
	{
		pLarge = (m_seg.IsLarge ? 1 : 0);
	}

	public void IsEphemeral(out int pEphemeral)
	{
		pEphemeral = (m_seg.IsEphemeral ? 1 : 0);
	}

	public void GetGen0Info(out ulong pStart, out ulong pLen)
	{
		pStart = m_seg.Gen0Start;
		pLen = m_seg.Gen0Length;
	}

	public void GetGen1Info(out ulong pStart, out ulong pLen)
	{
		pStart = m_seg.Gen1Start;
		pLen = m_seg.Gen1Length;
	}

	public void GetGen2Info(out ulong pStart, out ulong pLen)
	{
		pStart = m_seg.Gen2Start;
		pLen = m_seg.Gen2Length;
	}

	public void EnumerateObjects(out IMDObjectEnum ppEnum)
	{
		List<ulong> list = new List<ulong>();
		for (ulong num = m_seg.FirstObject; num != 0L; num = m_seg.NextObject(num))
		{
			list.Add(num);
		}
		ppEnum = new MDObjectEnum(list);
	}
}
