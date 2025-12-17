using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("3D450B70-75F8-49E0-ADB4-F4E6A33DF429")]
[TypeIdentifier]
public interface ILeakTrackInfo
{
	[DispId(1)]
	bool IsLeakTrackLoaded
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	[DispId(2)]
	double AllocationCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	[DispId(3)]
	double AllocationSize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	[DispId(4)]
	object LTTypesBySize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(5)]
	object LTTypesByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap1_4();

	[DispId(10)]
	object ModulesBySize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(10)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(11)]
	object ModulesByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(11)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap2_2();

	[DispId(14)]
	double Duration
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(14)]
		get;
	}

	[DispId(15)]
	double HandleCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(15)]
		get;
	}

	[DispId(16)]
	object HandleTypesByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(16)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap3_1();

	[DispId(18)]
	object HandleModulesByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(18)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}
}
