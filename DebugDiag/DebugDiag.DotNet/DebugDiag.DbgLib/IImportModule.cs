using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents a Imports Symbols in Modules.  This object is obtained from the ImportsInfo object.
/// For more information regarding the ImportsInfo COM object pelase see <see cref="T:DebugDiag.DbgLib.IImportsInfo" /> interface documentation.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IHangDumpRule, IAnalysisRuleMetadata
///             {
/// public void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetProgress progress)
/// {
///     DebugDiag.DbgLib.IModuleInfo ModuleInfo = debugger.Modules;
///
///     if (ModuleInfo == null)
///     {
///         manager.WriteLine("Unable to obtain module info");
///     }
///     else
///     {
///         //Navigate through the modules collection to print the modules name
///         foreach (IDbgModule Module in ModuleInfo)
///         {
///             foreach (IImportModule ImportModule in Module.ImportsInfo)
///             {
///                 if(ImportModule.Name.Contains("MSVBM"))
///                 {
///                     manager.WriteLine("Found VB module " + ImportModule.Name + " on Module - " + Module.ModuleName);
///                 }   
///             }       
///         }
///     }        
/// }
///             }  
/// </code>
/// </example>
[ComImport]
[Guid("0CD7733B-8714-4DFF-A036-B0DC6DBBB4FD")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IImportModule : IEnumerable
{
	/// <summary>
	/// This property returns the name of the module. 
	/// </summary>
	[DispId(1)]
	string Name
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the number of import symbols in the modules contained in the dump file. 
	/// </summary>
	[DispId(2)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	/// <summary>
	/// This property returns the import functions for the modules contained in the dump file.
	/// </summary>
	/// <param name="Index">The method takes an integer that is a zero based index of the function to retrieve.</param>
	/// <returns>Returns a string with the function name from the symbols</returns>
	[DispId(0)]
	string this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of function names
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
