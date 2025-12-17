using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("86E46E0C-EC70-4CAE-98AE-FDAF668EEE83")]
[TypeIdentifier]
public interface ILTFunction
{
	[DispId(1)]
	double AllocationCount
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

	[DispId(3)]
	int LeakProbability
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	[DispId(4)]
	int CallStackCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	[DispId(5)]
	object CallStack
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(6)]
	object SizeInfosBySize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(7)]
	object SizeInfosByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap1_1();

	[DispId(9)]
	int AllocationType
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		get;
	}

	[DispId(10)]
	object LTHeapsBySize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(10)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(11)]
	object LTHeapsByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(11)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap2_1();

	[DispId(13)]
	IMemoryAllocation MemoryAllocation
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(13)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(14)]
	ILTHandleAllocation HandleAllocation
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(14)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}
}
