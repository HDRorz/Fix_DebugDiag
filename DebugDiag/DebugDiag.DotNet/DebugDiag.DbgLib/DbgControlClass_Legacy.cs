using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FCanCreate)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("4001052F-9B7B-46A6-AD4B-C6984222BE62")]
internal class DbgControlClass_Legacy : IDbgControl, DbgControl_Legacy
{
	[DispId(4)]
	public virtual extern IAnalyzer Analyzer
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(4)]
	extern IAnalyzer IDbgControl.Analyzer
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(5)]
	extern bool IDbgControl.RawLogging
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[param: In]
		set;
	}

	[DispId(5)]
	public virtual extern bool RawLogging
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[param: In]
		set;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	public virtual extern void AttachToProcess([In] int ProcessID, [In][MarshalAs(UnmanagedType.BStr)] string ScriptPath, [In][MarshalAs(UnmanagedType.BStr)] string SymbolPath, [In][MarshalAs(UnmanagedType.BStr)] string DumpPath);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	public virtual extern void DetachFromProcess();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	[return: MarshalAs(UnmanagedType.Interface)]
	public virtual extern IDbgObj4 OpenDump([In][MarshalAs(UnmanagedType.BStr)] string DumpPath, [In][MarshalAs(UnmanagedType.BStr)] string SymbolPath, [In][MarshalAs(UnmanagedType.BStr)] string ImagePath, [In][MarshalAs(UnmanagedType.IUnknown)] object pProgress);
}
