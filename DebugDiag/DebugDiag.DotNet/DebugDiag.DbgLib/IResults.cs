using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents a global collection of Result objects. 
/// An instance of this object is obtained from the <c>NetScriptManager.GetResults</c> property of the Manager object.  
/// The Manager object is an intrinsic object of the script and available by default.
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
[Guid("B8DF5BB4-B171-44C7-9BE7-A1D03800C50F")]
public interface IResults : IEnumerable
{
	/// <summary>
	/// This property returns an Integer value of number of results (Errors/Warnings/Information) reported by the Analyzer.
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns an instance of the Result object.
	/// </summary>
	/// <param name="Index">Zero based index of the Result to retrieve</param>
	/// <returns>Returns and instance of a COM Result object that implements the <see cref="T:DebugDiag.DbgLib.IResult" /> interface</returns>
	[DispId(0)]
	IResult this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of Result COM objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
