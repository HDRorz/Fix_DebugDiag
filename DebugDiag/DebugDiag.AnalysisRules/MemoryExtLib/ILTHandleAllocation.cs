using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("7119DD00-676B-4270-A458-429D4B5B0554")]
[TypeIdentifier]
public interface ILTHandleAllocation
{
	[DispId(1)]
	double Handle
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	void _VtblGap1_1();

	[DispId(3)]
	double AllocationTime
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}
}
