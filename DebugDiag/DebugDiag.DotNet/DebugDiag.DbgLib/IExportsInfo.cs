using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that maintains a collection of ExportSymbol objects for a specific DLL module in the dump file. 
/// An instance of this object is obtained from the <c>IDbgModule.ExportsInfo</c> property. For more information about the ExportSymbol COM object please
/// see <see cref="T:DebugDiag.DbgLib.IExportSymbol" /> interface documentation.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IMultiDumpRule, IAnalysisRuleMetadata
///             {
/// public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
/// {
///    //Create a list of dumps that will be analyzed
///    List&lt;string&gt; dumpFiles = new List&lt;string&gt;();
///
///    dumpFiles = manager.GetDumpFiles();
///
///    foreach (string dump in dumpFiles)
///    {
///        using (NetDbgObj debugger = manager.GetDebugger(dump))
///        {
///            DebugDiag.DbgLib.IDbgModule Module = debugger.GetModuleByModuleName("clr");
///
///            if (Module == null)
///            {
///                manager.WriteLine("Unable to obtain Module object for clr.dll");
///            }
///            else
///            {
///                 manager.WriteLine("Export information for  " + Module.ImageName + "&lt;/BR\&gt;");
///
///                 DebugDiag.DbgLib.IExportsInfo ExportInfo = Module.ExportsInfo;
///
///                 foreach (dynamic Symbol in ExportInfo)
///                 { 
///                     manager.WriteLine( "SymbolName: " + Symbol.SymbolName);
///                     manager.WriteLine( "Address: " + debugger.GetAs32BitHexString(Symbol.Address));
///                     manager.WriteLine( "Ordinal: " + Symbol.Ordinal.ToString());
///                     manager.WriteLine("RVA: " + debugger.GetAs32BitHexString(Symbol.RVA) +"&lt;/BR&gt;");
///
///                 }
///             }
///        }
///    }
/// }
///             }        
/// </code>
/// </example>
[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("9F33608F-B2A9-48BD-946C-F3E42AC6495D")]
public interface IExportsInfo : IEnumerable
{
	/// <summary>
	/// Returns the number of ExportSymbol COM objects maintained by this collection
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// Returns an instance of a ExportSymbol COM object based on the index passed into the property.
	/// </summary>
	/// <param name="Index"></param>
	/// <returns></returns>
	[DispId(0)]
	IExportSymbol this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of ExportSymbol COM objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
