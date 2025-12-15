using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that manages the information for all locked critical sections in a dump file.  
/// An instance of this object is obtained by calling the <c>NetDbgObj.CritSecInfo</c> method.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IMultiDumpRule, IAnalysisRuleMetadata
///             {
///    public void RunAnalysisRule(DebugDiag.DotNet.NetScriptManager manager, DebugDiag.DotNet.NetProgress Progress)
///    {
///
///        //Create a list of dumps that will be analyzed
///        List&lt;string&gt; dumpFiles = new List&lt;string&gt;();
///
///        dumpFiles = manager.GetDumpFiles();
///
///        foreach (string dump in dumpFiles)
///        {
///            using (NetDbgObj debugger = manager.GetDebugger(dump))
///            {
///                DebugDiag.DbgLib.ICritSecInfo CritSecInfo = debugger.CritSecs;
///
///                if (CritSecInfo.Count == 0)
///                {
///                    manager.Write("No locked critical sections");
///                }
///                else 
///                {
///                    foreach (DebugDiag.DbgLib.IDbgCritSec DbgCritSec in CritSecInfo)
///                   { 
///                        manager.WriteLine("CritSec " + debugger.GetSymbolFromAddress(DbgCritSec.Address) + " at " + DbgCritSec.Address.ToString());
///                        manager.WriteLine( "There are " + DbgCritSec.LockCount.ToString() + " locks on this critical section.");
///                    }
///                }
///            }
///        }
///    }
///             }
/// </code>
/// </example>
[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("27569C24-82BA-44FF-9D88-AC6EE6A8F0DA")]
public interface ICritSecInfo : IEnumerable
{
	/// <summary>
	/// This property returns the number of locked critical sections in the dump file.
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns a COM DbgCritSec object that can be used to query information about a critical section. 
	/// </summary>
	/// <param name="Index">Integer with the index of the item to retrieve from the collection</param>
	/// <returns>An instance of a COM object that implements the <see cref="T:DebugDiag.DbgLib.IDbgCritSec" />Interface</returns>
	[DispId(0)]
	IDbgCritSec this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of DbgCritSec COM objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();

	/// <summary>
	/// This method initializes the CritSecInfo COM object. This method is called automatically when you obtain the count via the Count Property.
	/// </summary>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	void LoadAllLockedCriticalSections();

	/// <summary>
	/// This method initializes and returns a DbgCritSec COM object based on the address passed into the method.   
	/// The DbgCritSec object is used to retrieve information about the critical section it represents.
	/// See the <c>IDbgCritSec</c> interface documentation for more information about the DbgCritSec object.
	/// </summary>
	/// <param name="Address">The address passed into the method should be the address of a critical section.</param>
	/// <returns>An instance of a COM object implementing the <see cref="T:DebugDiag.DbgLib.IDbgCritSec" /> Interface</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	[return: MarshalAs(UnmanagedType.Interface)]
	IDbgCritSec GetCritSecByAddress([In] double Address);
}
