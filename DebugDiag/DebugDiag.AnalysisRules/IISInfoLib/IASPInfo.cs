using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IISInfoLib;

[ComImport]
[CompilerGenerated]
[Guid("77766AB2-D821-48D0-ABEB-63FB4EC155BE")]
[TypeIdentifier]
public interface IASPInfo
{
	[DispId(1)]
	IASPCurrentRequests CurrentRequests
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(2)]
	IASPTemplateCache TemplateCache
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(3)]
	IASPApps ASPApps
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(4)]
	[return: MarshalAs(UnmanagedType.Interface)]
	IASPRequest GetASPRequestByThreadID([In] int ThreadID);
}
