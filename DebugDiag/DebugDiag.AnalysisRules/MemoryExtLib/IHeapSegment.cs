using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("FD288D2F-6B6B-417B-87B9-AD685958D69C")]
[TypeIdentifier]
public interface IHeapSegment
{
	[DispId(1)]
	double BaseAddress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	[DispId(2)]
	double LargestUnCommittedRange
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	[DispId(3)]
	double NumberOfPages
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	void _VtblGap1_2();

	[DispId(6)]
	double NumberOfUnCommittedRanges
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		get;
	}

	[DispId(7)]
	double NumberOfUnCommittedPages
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		get;
	}
}
