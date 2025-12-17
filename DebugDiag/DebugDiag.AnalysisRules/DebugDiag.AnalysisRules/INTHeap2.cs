using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

[DefaultMember("Item")]
[Guid("B723A4F1-D44D-470C-BA03-470201B6B4B0")]
[TypeLibType(4288)]
public interface INTHeap2 : IEnumerable
{
	[DispId(1)]
	double ReservedMemory { get; }

	[DispId(2)]
	double CommittedMemory { get; }

	[DispId(3)]
	double Handle { get; }

	[DispId(4)]
	int Index { get; }

	[DispId(5)]
	double LargestUnCommittedRange { get; }

	[DispId(6)]
	double NumberOfUnCommittedRanges { get; }

	[DispId(7)]
	int Count { get; }

	[DispId(8)]
	dynamic BusyStatisticsByCount { get; }

	[DispId(9)]
	dynamic BusyStatisticsBySize { get; }

	[DispId(11)]
	string Name { get; }

	[DispId(12)]
	string Description { get; }

	[DispId(13)]
	string HeapType { get; }

	[DispId(-4)]
	new IEnumerator GetEnumerator();

	[DispId(0)]
	IHeapSegment Item(int Index);

	[DispId(10)]
	dynamic FindAllocations(double AllocationSize, int SearchLimit);
}
