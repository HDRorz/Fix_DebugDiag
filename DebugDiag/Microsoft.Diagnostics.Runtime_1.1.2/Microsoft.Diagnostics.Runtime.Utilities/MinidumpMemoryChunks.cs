using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class MinidumpMemoryChunks
{
	private MinidumpMemory64List _memory64List;

	private MinidumpMemoryList _memoryList;

	private MinidumpMemoryChunk[] _chunks;

	private readonly DumpPointer _dumpStream;

	private readonly MINIDUMP_STREAM_TYPE _listType;

	public ulong Count { get; private set; }

	public ulong Size(ulong i)
	{
		return _chunks[i].Size;
	}

	public ulong RVA(ulong i)
	{
		return _chunks[i].RVA;
	}

	public ulong StartAddress(ulong i)
	{
		return _chunks[i].TargetStartAddress;
	}

	public ulong EndAddress(ulong i)
	{
		return _chunks[i].TargetEndAddress;
	}

	public int GetChunkContainingAddress(ulong address)
	{
		MinidumpMemoryChunk value = new MinidumpMemoryChunk
		{
			TargetStartAddress = address
		};
		int num = Array.BinarySearch(_chunks, value);
		if (num >= 0)
		{
			return num;
		}
		if (~num != 0)
		{
			int num2 = Math.Min(_chunks.Length, ~num) - 1;
			if (_chunks[num2].TargetStartAddress <= address && _chunks[num2].TargetEndAddress > address)
			{
				return num2;
			}
		}
		return -1;
	}

	public MinidumpMemoryChunks(DumpPointer rawStream, MINIDUMP_STREAM_TYPE type)
	{
		Count = 0uL;
		_memory64List = null;
		_memoryList = null;
		_listType = MINIDUMP_STREAM_TYPE.UnusedStream;
		if (type != MINIDUMP_STREAM_TYPE.MemoryListStream && type != MINIDUMP_STREAM_TYPE.Memory64ListStream)
		{
			throw new ClrDiagnosticsException("Type must be either MemoryListStream or Memory64ListStream", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		_listType = type;
		_dumpStream = rawStream;
		if (MINIDUMP_STREAM_TYPE.Memory64ListStream == type)
		{
			InitFromMemory64List();
		}
		else
		{
			InitFromMemoryList();
		}
	}

	private void InitFromMemory64List()
	{
		_memory64List = new MinidumpMemory64List(_dumpStream);
		RVA64 baseRva = _memory64List.BaseRva;
		ulong count = _memory64List.Count;
		List<MinidumpMemoryChunk> list = new List<MinidumpMemoryChunk>();
		for (ulong num = 0uL; num < count; num++)
		{
			MINIDUMP_MEMORY_DESCRIPTOR64 element = _memory64List.GetElement((uint)num);
			if (element.DataSize != 0)
			{
				MinidumpMemoryChunk item = new MinidumpMemoryChunk
				{
					Size = element.DataSize,
					TargetStartAddress = element.StartOfMemoryRange,
					TargetEndAddress = element.StartOfMemoryRange + element.DataSize,
					RVA = baseRva.Value
				};
				baseRva.Value += element.DataSize;
				list.Add(item);
			}
		}
		list.Sort();
		SplitAndMergeChunks(list);
		_chunks = list.ToArray();
		Count = (ulong)list.Count;
		ValidateChunks();
	}

	public void InitFromMemoryList()
	{
		_memoryList = new MinidumpMemoryList(_dumpStream);
		uint count = _memoryList.Count;
		List<MinidumpMemoryChunk> list = new List<MinidumpMemoryChunk>();
		for (ulong num = 0uL; num < count; num++)
		{
			MinidumpMemoryChunk minidumpMemoryChunk = new MinidumpMemoryChunk();
			MINIDUMP_MEMORY_DESCRIPTOR element = _memoryList.GetElement((uint)num);
			if (element.Memory.DataSize != 0)
			{
				minidumpMemoryChunk.Size = element.Memory.DataSize;
				minidumpMemoryChunk.TargetStartAddress = element.StartOfMemoryRange;
				minidumpMemoryChunk.TargetEndAddress = element.StartOfMemoryRange + element.Memory.DataSize;
				minidumpMemoryChunk.RVA = element.Memory.Rva.Value;
				list.Add(minidumpMemoryChunk);
			}
		}
		list.Sort();
		SplitAndMergeChunks(list);
		_chunks = list.ToArray();
		Count = (ulong)list.Count;
		ValidateChunks();
	}

	private void SplitAndMergeChunks(List<MinidumpMemoryChunk> chunks)
	{
		for (int i = 1; i < chunks.Count; i++)
		{
			MinidumpMemoryChunk minidumpMemoryChunk = chunks[i - 1];
			MinidumpMemoryChunk minidumpMemoryChunk2 = chunks[i];
			if (minidumpMemoryChunk.TargetEndAddress <= minidumpMemoryChunk2.TargetStartAddress)
			{
				continue;
			}
			if (minidumpMemoryChunk.TargetEndAddress >= minidumpMemoryChunk2.TargetEndAddress)
			{
				chunks.RemoveAt(i);
				i--;
				continue;
			}
			ulong num = minidumpMemoryChunk.TargetEndAddress - minidumpMemoryChunk2.TargetStartAddress;
			minidumpMemoryChunk2.TargetStartAddress += num;
			minidumpMemoryChunk2.RVA += num;
			minidumpMemoryChunk2.Size -= num;
			int j;
			for (j = i; j < chunks.Count - 1 && minidumpMemoryChunk2.TargetStartAddress > chunks[j + 1].TargetStartAddress; j++)
			{
			}
			if (j != i)
			{
				chunks.RemoveAt(i);
				chunks.Insert(j - 1, minidumpMemoryChunk2);
				i--;
			}
		}
	}

	private void ValidateChunks()
	{
		for (ulong num = 0uL; num < Count; num++)
		{
			if (_chunks[num].Size != _chunks[num].TargetEndAddress - _chunks[num].TargetStartAddress || _chunks[num].TargetStartAddress > _chunks[num].TargetEndAddress)
			{
				throw new ClrDiagnosticsException("Unexpected inconsistency error in dump memory chunk " + num + " with target base address " + _chunks[num].TargetStartAddress + ".", ClrDiagnosticsExceptionKind.CrashDumpError);
			}
			if (num < Count - 1 && _listType == MINIDUMP_STREAM_TYPE.Memory64ListStream && (_chunks[num].RVA >= _chunks[num + 1].RVA || _chunks[num].TargetEndAddress > _chunks[num + 1].TargetStartAddress))
			{
				throw new ClrDiagnosticsException("Unexpected relative addresses inconsistency between dump memory chunks " + num + " and " + (num + 1) + ".", ClrDiagnosticsExceptionKind.CrashDumpError);
			}
			if (num < Count - 1 && _chunks[num].TargetEndAddress > _chunks[num + 1].TargetStartAddress)
			{
				throw new ClrDiagnosticsException("Unexpected overlap between memory chunks", ClrDiagnosticsExceptionKind.CrashDumpError);
			}
		}
	}
}
