using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class ClrDataMethod : CallableCOMWrapper
{
	private delegate int GetILAddressMapDelegate(IntPtr self, uint mapLen, out uint mapNeeded, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ILToNativeMap[] map);

	private static Guid IID_IXCLRDataMethodInstance = new Guid("ECD73800-22CA-4b0d-AB55-E9BA7E6318A5");

	private GetILAddressMapDelegate _getILAddressMap;

	private unsafe IXCLRDataMethodInstanceVTable* VTable => (IXCLRDataMethodInstanceVTable*)base._vtable;

	public ClrDataMethod(DacLibrary library, IntPtr pUnk)
		: base(library.OwningLibrary, ref IID_IXCLRDataMethodInstance, pUnk)
	{
	}

	public unsafe ILToNativeMap[] GetILToNativeMap()
	{
		CallableCOMWrapper.InitDelegate(ref _getILAddressMap, VTable->GetILAddressMap);
		if (_getILAddressMap(base.Self, 0u, out var mapNeeded, null) != 0)
		{
			return null;
		}
		ILToNativeMap[] array = new ILToNativeMap[mapNeeded];
		if (_getILAddressMap(base.Self, mapNeeded, out mapNeeded, array) != 0)
		{
			return null;
		}
		return array;
	}
}
