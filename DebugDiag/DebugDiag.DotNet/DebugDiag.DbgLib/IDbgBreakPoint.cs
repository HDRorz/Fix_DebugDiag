using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents a breakpoint.  This object is obtained from the <c>NetDbgObj.GetBreakPoint</c> property
/// and is used on live debugging.
/// </summary>
[ComImport]
[Guid("C100B089-1682-4F93-BA86-7AB4531DC9FD")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IDbgBreakPoint
{
	/// <summary>
	/// This property returns the unique sequential id associated with this breakpoint.
	/// </summary>
	[DispId(1)]
	int ID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns “Code” if this is a code breakpoint or “Data” if this is a data breakpoint. 
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
	/// This property returns the address where the breakpoint has been set if this is a non-deferred breakpoint.
	/// </summary>
	[DispId(3)]
	double Offset
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	/// <summary>
	/// This property returns the size of the data breakpoint in bytes.  For example, the value of 4 would be returned for a DWORD data breakpoint.
	/// </summary>
	[DispId(4)]
	int DataSize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	/// <summary>
	/// This property returns a combination of “Read”, “Write” and “Execute” depending on the access type of the data breakpoint.
	/// </summary>
	[DispId(5)]
	string DataAccessType
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns a number that specifies how many times this breakpoint hit should be ignored before triggering a breakpoint event.
	/// </summary>
	[DispId(6)]
	double PassCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		get;
	}

	/// <summary>
	/// This property returns the number of times this breakpoint has been passed.  Each time a breakpoint is hit but not triggered this value is incremented by 1. 
	/// </summary>
	[DispId(7)]
	double CurrentPassCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		get;
	}

	/// <summary>
	/// This property returns a valid thread id if the breakpoint is thread specific.
	/// </summary>
	[DispId(8)]
	double MatchThreadID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(8)]
		get;
	}

	/// <summary>
	/// This property returns the debugger command associated with this breakpoint.
	/// </summary>
	[DispId(9)]
	string Command
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		[param: In]
		[param: MarshalAs(UnmanagedType.BStr)]
		set;
	}

	/// <summary>
	/// This property returns true if the breakpoint resolution is deferred since the module is not yet loaded, otherwise it returns false.
	/// </summary>
	[DispId(10)]
	bool IsDeferred
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(10)]
		get;
	}

	/// <summary>
	/// This property returns the address/symbol expression used to create the breakpoint. 
	/// </summary>
	[DispId(11)]
	string OffsetExpression
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(11)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}
}
