using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that maintains a collection of DbgStackFrame COM objects.  
/// An instance of this object is obtained from the <c>NetDbgThread.StackFrames</c> property.
/// </summary>
[ComImport]
[Guid("E0297013-CF70-4760-B5C1-4E8454FD133E")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IStackFrames : IEnumerable
{
	/// <summary>
	/// This property returns a count of the number of stack frames managed by this object. 
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns a DbgStackFrame object that can be used to retrieve specific information about the stack frame. 
	/// </summary>
	/// <param name="Index"></param>
	/// <returns></returns>
	[DispId(0)]
	IDbgStackFrame this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of IDbgStackFrame COM objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
