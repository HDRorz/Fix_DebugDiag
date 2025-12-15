using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("273BDA93-B5DA-4CB2-AD1E-937FF1456328")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IModuleInfo : IEnumerable
{
	/// <summary>
	/// This property returns a count that represents the number of modules contained in the dump file.
	/// </summary>
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns an instance of the DbgModule object. The method takes an Integer that is a zero based index of the module to retrieve.
	/// </summary>
	/// <param name="Index">Zero based index of the module to retrieve</param>
	/// <returns>Returns and instance of a COM DbgModule object that implements the <see cref="T:DebugDiag.DbgLib.IDbgModule" /> interface.</returns>
	[DispId(0)]
	IDbgModule this[int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This method gives access to the enumerator for the collection of DbgModule COM objects
	/// </summary>
	/// <returns>Enumrator that implements the IEnumerator .Net interface.</returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "")]
	new IEnumerator GetEnumerator();

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	[return: MarshalAs(UnmanagedType.Struct)]
	object GetModulesBySize();
}
