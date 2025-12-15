using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This interface is implemented by a COM object represents a critical section.  This object is obtained from the CritSecInfo object.
/// More information about the CritSecInfo objects can be found in the <see cref="T:DebugDiag.DbgLib.ICritSecInfo" /> Interface documentation.
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
///                        manager.Write("Critical Section Information&lt;/BR&gt;");
///                        manager.Write("&lt;/BR&lt;CritSec " + debugger.GetSymbolFromAddress(DbgCritSec.Address) + " at " + DbgCritSec.Address.ToString());
///                        manager.Write("&lt;/BR&lt;LockCount " + DbgCritSec.LockCount.ToString());
///                        manager.Write("&lt;/BR&lt;RecursionCount " + DbgCritSec.RecursionCount.ToString());
///                        manager.Write("&lt;/BR&lt;OwnerThreadID - Debugger " + DbgCritSec.OwnerThreadID.ToString());
///                        manager.Write("&lt;/BR&lt;OwningThreadID - System " + DbgCritSec.OwnerThreadSystemID.ToString());
///                        manager.Write("&lt;/BR&lt;EntryCount " + DbgCritSec.EntryCount.ToString());
///                        manager.Write("&lt;/BR&lt;ContentionCount " + DbgCritSec.ContentionCount.ToString());
///                        manager.Write("&lt;/BR&lt;SpinCount " + DbgCritSec.SpinCount.ToString());
///                        manager.Write("&lt;/BR&lt;State " + DbgCritSec.State);
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
[Guid("95C9ECAE-3C1E-4302-BCA8-AB55E377E960")]
public interface IDbgCritSec
{
	/// <summary>
	/// This property returns the address of the critical section. 
	/// If symbols are loaded you can pass the address of the critical section to <c>NetDbgObj.GetSymbolFromAddress</c> to determine who owns this critical section.
	/// </summary>
	[DispId(1)]
	double Address
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns the lock count value of the critical section.
	/// </summary>
	[DispId(2)]
	int LockCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	/// <summary>
	/// This property returns the contention count value of the critical section.
	/// </summary>
	[DispId(3)]
	double ContentionCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	/// <summary>
	/// This property returns the entry count value of the critical section.
	/// </summary>
	[DispId(4)]
	double EntryCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	/// <summary>
	/// This property returns the debugger thread ID of the thread that currently owns this critical section. 
	/// </summary>
	[DispId(5)]
	int OwnerThreadID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}

	/// <summary>
	/// This property returns the system thread ID of the thread that currently owns this critical section. 
	/// </summary>
	[DispId(6)]
	double OwnerThreadSystemID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		get;
	}

	/// <summary>
	/// This property returns the spin count value of the critical section.
	/// </summary>
	[DispId(7)]
	double SpinCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		get;
	}

	/// <summary>
	/// This property returns a string value that represents the current state of the critical section.
	///
	/// This value will be one of the following values:
	///
	/// Locked
	/// Unlocked
	/// Transitioning
	/// Orphaned
	/// Deadlocked
	/// Uninitialized 
	/// </summary>
	[DispId(8)]
	string State
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(8)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the recursion count value of the critical section.
	/// </summary>
	[DispId(9)]
	int RecursionCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		get;
	}
}
