using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("DA70E7F1-BCB1-40D4-AEAA-16EC0DF3C1E3")]
public interface IDbgState
{
	/// <summary>
	/// This method returns an object represented by the Key value in the collection.
	/// </summary>
	/// <param name="Key">String representing the key for the object on the collection</param>
	/// <returns>Returns an Object for the object found using the key parameter</returns>
	[DispId(0)]
	object this[string Key]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[param: In]
		[param: MarshalAs(UnmanagedType.Struct)]
		set;
	}

	/// <summary>
	/// This method allows the user to store a value on the collection using a unique key parameter
	/// </summary>
	/// <param name="Key">String key that represents the value being stored on the collection</param>
	/// <param name="pVal">Value to store</param>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(0)]
	void let_Value([In][MarshalAs(UnmanagedType.BStr)] string Key, [In][MarshalAs(UnmanagedType.Struct)] object pVal);

	/// <summary>
	/// This method removes the specified key value pair from the property bag. 
	/// </summary>
	/// <param name="Key">String key that represents the value stored</param>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	void Remove([In][MarshalAs(UnmanagedType.BStr)] string Key);

	/// <summary>
	/// This method removes all of the specified key value pairs from the property bag. 
	/// </summary>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	void RemoveAll();
}
