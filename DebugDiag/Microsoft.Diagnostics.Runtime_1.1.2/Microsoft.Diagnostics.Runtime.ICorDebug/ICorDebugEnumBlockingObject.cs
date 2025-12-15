using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("976A6278-134A-4a81-81A3-8F277943F4C3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ICorDebugEnumBlockingObject : ICorDebugEnum
{
	new void Skip([In] uint countElements);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum enumerator);

	new void GetCount(out uint countElements);

	[PreserveSig]
	int Next([In] uint countElements, [Out][MarshalAs(UnmanagedType.LPArray)] CorDebugBlockingObject[] blockingObjects, out uint countElementsFetched);
}
