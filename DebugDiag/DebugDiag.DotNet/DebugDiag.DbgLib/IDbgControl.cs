using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("429DB77C-C4C7-4A3C-8699-817EA88903E0")]
public interface IDbgControl
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	void AttachToProcess([In] int ProcessID, [In][MarshalAs(UnmanagedType.BStr)] string ScriptPath, [In][MarshalAs(UnmanagedType.BStr)] string SymbolPath, [In][MarshalAs(UnmanagedType.BStr)] string DumpPath);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	void DetachFromProcess();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	[return: MarshalAs(UnmanagedType.Interface)]
	IDbgObj4 OpenDump([In][MarshalAs(UnmanagedType.BStr)] string DumpPath, [In][MarshalAs(UnmanagedType.BStr)] string SymbolPath, [In][MarshalAs(UnmanagedType.BStr)] string ImagePath, [In][MarshalAs(UnmanagedType.IUnknown)] object pProgress);

	[DispId(4)]
	IAnalyzer Analyzer
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(5)]
	bool RawLogging
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[param: In]
		set;
	}
}
