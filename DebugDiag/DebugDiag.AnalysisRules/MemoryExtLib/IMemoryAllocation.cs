using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("7DBD7FD8-CAFC-4770-980A-C74B04D94F4D")]
[TypeIdentifier]
public interface IMemoryAllocation
{
	[DispId(1)]
	double AllocationTime
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	[DispId(2)]
	double AllocationSize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	void _VtblGap1_2();

	[DispId(5)]
	double AllocationAddress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}
}
