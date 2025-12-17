using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("154C425D-A727-4E70-BF79-0FA1DF88E5D0")]
[TypeIdentifier]
public interface IVMInfo
{
	[DispId(1)]
	IHeapInfo HeapInfo
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	void _VtblGap1_1();

	[DispId(3)]
	ILeakTrackInfo LeakTrackInfo
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(4)]
	int GetFilteredBlockCount([In] int RegionUsage, [In] int RegionType, [In] int RegionState, [In] double Extra);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(5)]
	double GetFilteredBlockSize([In] int RegionUsage, [In] int RegionType, [In] int RegionState, [In] double Extra);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(6)]
	double GetLargestFilteredBlockSize([In] int RegionUsage, [In] int RegionType, [In] int RegionState, [In] double Extra);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(7)]
	double GetLargestFilteredBlockAddress([In] int RegionUsage, [In] int RegionType, [In] int RegionState, [In] double Extra);
}
