using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents a Result object. An instance of this object is obtained from the Results object.
/// For more infomration regarding the Results object please see <see cref="T:DebugDiag.DbgLib.IResults" /> interface documentation.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IHangDumpRule, IAnalysisRuleMetadata
///             {
/// public void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetProgress progress)
/// {
///     //Obtain the Results Collection Reference
///     IResults Results = manager.GetResults(0);
///
///     manager.ReportError("This is an Error", "This is the Solution");
///     manager.ReportWarning("This is a Warning", "This is a Suggestion");
///     manager.ReportInformation("This is for your Information");
///
///     //Navigates de Collection to show the content of each result
///     foreach (IResult Result in Results)
///     { 
///         manager.WriteLine("Description = " + Result.Description);
///         manager.WriteLine("Recommendation = " + Result.Recommendation);
///         manager.WriteLine("Source = " + Result.Source);
///         manager.WriteLine("Type = " + Result.Type + "&lt;/BR&gt;");
///     }       
/// }
///             } 
/// </code>
/// </example>
[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("4C6790CA-2598-48AE-AAC6-43CAFFB7175C")]
public interface IResult
{
	/// <summary>
	/// This property returns a string value for the source of the result. 
	/// </summary>
	[DispId(1)]
	string Source
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns a string value for Type of results (Error/Warning/Information).
	/// </summary>
	[DispId(2)]
	string Type
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns a string value of the result description. 
	/// </summary>
	[DispId(3)]
	string Description
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns a string value for the recommendations to make based on results.
	/// </summary>
	[DispId(4)]
	string Recommendation
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property used as a means to sort the results based on importance. For Example: Result A with weight 10 will appear higher than a result B with weight 5 in the analysis summary.
	/// </summary>
	[DispId(5)]
	int Weight
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}

	[DispId(6)]
	string SolutionSourceID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}
}
