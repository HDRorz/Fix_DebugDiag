using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that contains information about a single exported symbol of a DLL modules exported symbols. 
/// An instance of this object is obtained from the ExportsInfo collection. For more information about the ExportsInfo collection please see <see cref="T:DebugDiag.DbgLib.IExportsInfo" /> interface
/// documentation.
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
///                 foreach (DebugDiag.DbgLib.IExportSymbol Symbol in ExportInfo)
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
[Guid("C953CF68-EBD4-4E9B-BBE6-955004B1980E")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IExportSymbol
{
	/// <summary>
	/// Returns a string containing the exported symbol name.
	/// </summary>
	[DispId(1)]
	string SymbolName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// Returns an integer value containing the ordinal of this exported symbol.
	/// </summary>
	[DispId(2)]
	int Ordinal
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	/// <summary>
	/// Returns an integer value containing the offset from the modules base address to this exported symbol. 
	/// Typically you would get the address of the exported symbol from the Address property, otherwise you could add the value from this property to the value returned 
	/// from <c>IDbgModule.Base</c> to calculate the address of the exported symbol.
	/// </summary>
	[DispId(3)]
	int RVA
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	[DispId(4)]
	int Hint
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	/// <summary>
	/// Returns a Double value containing the address of this exported symbol in the modules address space.
	/// </summary>
	[DispId(5)]
	double Address
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}
}
