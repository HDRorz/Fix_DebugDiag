using System;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class SOSDac6 : CallableCOMWrapper
{
	private delegate int DacGetMethodTableCollectibleData(IntPtr self, ulong addr, out MethodTableCollectibleData data);

	internal static Guid IID_ISOSDac6 = new Guid("11206399-4B66-4EDB-98EA-85654E59AD45");

	private DacGetMethodTableCollectibleData _getMethodTableCollectibleData;

	private unsafe ISOSDac6VTable* VTable => (ISOSDac6VTable*)base._vtable;

	public SOSDac6(DacLibrary library, IntPtr ptr)
		: base(library.OwningLibrary, ref IID_ISOSDac6, ptr)
	{
	}

	public SOSDac6(CallableCOMWrapper toClone)
		: base(toClone)
	{
	}

	public unsafe bool GetMethodTableCollectibleData(ulong addr, out MethodTableCollectibleData data)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodTableCollectibleData, VTable->GetMethodTableCollectibleData);
		return CallableCOMWrapper.SUCCEEDED(_getMethodTableCollectibleData(base.Self, addr, out data));
	}
}
