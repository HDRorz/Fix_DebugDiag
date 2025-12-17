using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IISInfoLib;

[ComImport]
[CompilerGenerated]
[Guid("09362F12-17A0-4391-BF0E-E7DF273BC9DC")]
[TypeIdentifier]
public interface IASPFile
{
	void _VtblGap1_1();

	[DispId(2)]
	IASPFile SiblingFile
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(3)]
	IASPFile ChildFile
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(4)]
	string PhysicalPath
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	[DispId(5)]
	DateTime LastModifiedTime
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}
}
