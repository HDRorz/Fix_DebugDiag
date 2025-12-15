using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that maintains a collection of environment variables that are associated with the debugger instance.  
/// An instance of this object is obtained from the <c>NetDbgObj.EnvironmentVariables</c> property.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IHangDumpRule, IAnalysisRuleMetadata
///             {
/// public void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetProgress progress)
/// {
///     DebugDiag.DbgLib.IEnvironmentVariables EnvironmentVars = debugger.EnvironmentVariables;
///
///     manager.WriteLine("Environment Variables: &lt;/BR&gt;");
///
///     foreach (dynamic StringVar in EnvironmentVars)
///     { 
///         manager.WriteLine(StringVar.ToString());
///     }         
///  }
///             } 
/// </code>
/// </example>
[ComImport]
[Guid("8550B76A-000B-46F8-9FD4-75D20A4A3A14")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IEnvironmentVariables : IEnumerable
{
	/// <summary>
	/// Returns the number of environment variables maintained by this collection.
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// Returns the value of the environment variable as a string based on the index passed into the property. 
	/// </summary>
	/// <param name="Index">&gt;The method takes an integer that is a zero based index of the Environment Variable to retrieve.</param>
	/// <returns>String with the name and value of the Environment Variable</returns>
	[DispId(0)]
	string this[object Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of environment variables
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
