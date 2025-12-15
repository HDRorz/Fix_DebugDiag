using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that manages the information for the imports symbols in modules in a dump file. 
/// An instance of this object is obtained by calling the <c>IModule.ImportsInfo</c> method.
/// </summary>
[ComImport]
[Guid("534F132D-B013-4756-8469-6D4DF9205AFE")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IImportsInfo : IEnumerable
{
	/// <summary>
	/// This property returns the number of import symbols in the modules contained in the dump file.
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns an instance of the ImportModule object. 
	/// </summary>
	/// <param name="Index">The method takes an integer that is a zero based index of the module to retrieve.</param>
	/// <returns>Returns a COM object that implements the <see cref="T:DebugDiag.DbgLib.IImportModule" /> interface.</returns>
	[DispId(0)]
	IImportModule this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of ImportModule COM Objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();
}
