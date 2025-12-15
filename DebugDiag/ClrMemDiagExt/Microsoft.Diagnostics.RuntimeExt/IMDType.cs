using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("FF5B59F4-07A0-4D7C-8B59-69338EECEA16")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDType
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetName([MarshalAs(UnmanagedType.BStr)] out string pName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetSize(ulong objRef, out ulong pSize);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void ContainsPointers(out int pContainsPointers);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetCorElementType(out int pCET);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetBaseType([MarshalAs(UnmanagedType.Interface)] out IMDType ppBaseType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetArrayComponentType([MarshalAs(UnmanagedType.Interface)] out IMDType ppArrayComponentType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetCCW(ulong addr, [MarshalAs(UnmanagedType.Interface)] out IMDCCW ppCCW);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRCW(ulong addr, [MarshalAs(UnmanagedType.Interface)] out IMDRCW ppRCW);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsArray(out int pIsArray);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsFree(out int pIsFree);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsException(out int pIsException);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void IsEnum(out int pIsEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetEnumElementType(out int pValue);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetEnumNames([MarshalAs(UnmanagedType.Interface)] out IMDStringEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetEnumValueInt32([MarshalAs(UnmanagedType.BStr)] string name, out int pValue);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetFieldCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetField(int index, [MarshalAs(UnmanagedType.Interface)] out IMDField ppField);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int GetFieldData(ulong obj, int interior, int count, [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] MD_FieldData[] fields, out int pNeeded);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetStaticFieldCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetStaticField(int index, [MarshalAs(UnmanagedType.Interface)] out IMDStaticField ppStaticField);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetThreadStaticFieldCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetThreadStaticField(int index, [MarshalAs(UnmanagedType.Interface)] out IMDThreadStaticField ppThreadStaticField);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetArrayLength(ulong objRef, out int pLength);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetArrayElementAddress(ulong objRef, int index, out ulong pAddr);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetArrayElementValue(ulong objRef, int index, [MarshalAs(UnmanagedType.Interface)] out IMDValue ppValue);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateReferences(ulong objRef, [MarshalAs(UnmanagedType.Interface)] out IMDReferenceEnum ppEnum);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateInterfaces([MarshalAs(UnmanagedType.Interface)] out IMDInterfaceEnum ppEnum);
}
