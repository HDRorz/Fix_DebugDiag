using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("ECD73800-22CA-4b0d-AB55-E9BA7E6318A5")]
internal interface IXCLRDataMethodInstance
{
	void GetTypeInstance_do_not_use();

	void GetDefinition_do_not_use();

	void GetTokenAndScope(out uint mdToken, [MarshalAs(UnmanagedType.Interface)] out object module);

	void GetName_do_not_use();

	void GetFlags_do_not_use();

	void IsSameObject_do_not_use();

	void GetEnCVersion_do_not_use();

	void GetNumTypeArguments_do_not_use();

	void GetTypeArgumentByIndex_do_not_use();

	void GetILOffsetsByAddress(ulong address, uint offsetsLen, out uint offsetsNeeded, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] ilOffsets);

	void GetAddressRangesByILOffset(uint ilOffset, uint rangesLen, out uint rangesNeeded, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] addressRanges);

	[PreserveSig]
	int GetILAddressMap(uint mapLen, out uint mapNeeded, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ILToNativeMap[] map);

	void StartEnumExtents_do_not_use();

	void EnumExtent_do_not_use();

	void EndEnumExtents_do_not_use();

	void Request_do_not_use();

	void GetRepresentativeEntryAddress_do_not_use();
}
