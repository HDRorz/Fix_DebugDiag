using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents a single frame of a call stack for a thread in the debugger. 
/// An instance of this object is retrieved from the StackFrames object. See <see cref="T:DebugDiag.DbgLib.IStackFrames" /> for more information on the StackFrames COM object.
/// </summary>
/// <example>
/// <code language="cs">
///
/// </code>
/// </example>
[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("D0497DDB-956C-4A1D-AA1E-3D2E6D2E5E0A")]
public interface IDbgStackFrame
{
	/// <summary>
	/// This property returns the return address for this stack frame. 
	/// </summary>
	[DispId(1)]
	double ReturnAddress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns the instruction address for this stack frame.
	/// </summary>
	[DispId(2)]
	double InstructionAddress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	/// <summary>
	/// This property returns the calculated value of ESP for this stack frame.
	/// </summary>
	[DispId(3)]
	double StackAddress
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	/// <summary>
	/// This property returns the number for this frame.  A value of zero indicates the top of the stack frame. 
	/// </summary>
	[DispId(4)]
	int FrameNumber
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	/// <summary>
	/// This property returns the value of EBP for this stack frame. 
	/// </summary>
	[DispId(5)]
	double ChildEBP
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		get;
	}

	/// <summary>
	/// This property returns the function arguments passed on the stack for this stack frame. 
	/// When calling this function pass in a value of 0,1 2, or 3 to get the 1st, 2nd, 3rd or 4th argument on the stack respectively.
	/// </summary>
	/// <param name="Index">Integer value containing the index of the parameter to retrun</param>
	/// <returns>Double that represents the value found on the address of the parameter</returns>
	[DispId(6)]
	double this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		get;
	}
}
