using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("8C7D4D10-D5B2-4C8D-921B-15FD9CAF9292")]
[TypeIdentifier]
public interface ILTModule
{
	[DispId(1)]
	double AllocationSize
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
	int LeakProbability
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	[DispId(4)]
	object FunctionsByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[DispId(5)]
	object FunctionsBySize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	void _VtblGap1_1();

	[DispId(7)]
	object LTTypesByCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}
}
