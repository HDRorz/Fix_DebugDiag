using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("1C07BA58-6A4F-4E44-90F2-A62056F379B9")]
public interface IManager2 : IManager
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	new void Write([In][MarshalAs(UnmanagedType.BStr)] string Output);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	new void WriteBlock([In] int BlockNum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	[return: MarshalAs(UnmanagedType.BStr)]
	new string ExecuteScript([In][MarshalAs(UnmanagedType.BStr)] string ScriptName, [In][MarshalAs(UnmanagedType.Struct)] object ScriptParam);

	[DispId(4)]
	new IDataFiles DataFiles
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(5)]
	new void ReportInformation([In][MarshalAs(UnmanagedType.BStr)] string Information, [In] int Weight = 0, [In][MarshalAs(UnmanagedType.BStr)] string SolutionSourceID = "");

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(6)]
	new void ReportWarning([In][MarshalAs(UnmanagedType.BStr)] string Warning, [In][MarshalAs(UnmanagedType.BStr)] string Recommendation, [In] int Weight = 0, [In][MarshalAs(UnmanagedType.BStr)] string SolutionSourceID = "");

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(7)]
	new void ReportError([In][MarshalAs(UnmanagedType.BStr)] string Error, [In][MarshalAs(UnmanagedType.BStr)] string Recommendation, [In] int Weight = 0, [In][MarshalAs(UnmanagedType.BStr)] string SolutionSourceID = "");

	[DispId(8)]
	new IResults Results
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(8)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(9)]
	new IScripts Scripts
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(10)]
	[return: MarshalAs(UnmanagedType.IDispatch)]
	new object GetDebugger([In][MarshalAs(UnmanagedType.BStr)] string DumpFile);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(11)]
	new void CloseDebugger([In][MarshalAs(UnmanagedType.BStr)] string DataFile);

	[DispId(12)]
	new IProgress Progress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(12)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(13)]
	new object ScriptParameter
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(13)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(14)]
	new void End([In][MarshalAs(UnmanagedType.BStr)] string ErrorSource, [In][MarshalAs(UnmanagedType.BStr)] string ErrorDescription);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(15)]
	new void ReportOther([In][MarshalAs(UnmanagedType.BStr)] string Description, [In][MarshalAs(UnmanagedType.BStr)] string Recommendation, [In][MarshalAs(UnmanagedType.BStr)] string TypeLabel, [In][MarshalAs(UnmanagedType.BStr)] string IconFileName, [In] int Weight = 0, [In][MarshalAs(UnmanagedType.BStr)] string SolutionSourceID = "");

	[DispId(16)]
	string CurrentAnalysisRule
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(16)]
		[param: In]
		[param: MarshalAs(UnmanagedType.BStr)]
		set;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(17)]
	void WriteReportFile([MarshalAs(UnmanagedType.Interface)] IReportCallbacks pReportCallbacks);
}
