using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("1C07BA58-6A4F-4E44-90F2-A62056F379BB")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IReportCallbacks
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	[return: MarshalAs(UnmanagedType.BStr)]
	string GetResource([In][MarshalAs(UnmanagedType.BStr)] string ResourceName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	string GetScriptsSummaryTableContents();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	string GetReportFileFullPath();
}
