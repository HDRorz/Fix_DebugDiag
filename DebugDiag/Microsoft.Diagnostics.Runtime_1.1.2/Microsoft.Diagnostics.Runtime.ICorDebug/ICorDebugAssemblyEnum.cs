using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("4A2A1EC9-85EC-4BFB-9F15-A89FDFE0FE83")]
[ComConversionLoss]
[InterfaceType(1)]
public interface ICorDebugAssemblyEnum : ICorDebugEnum
{
	new void Skip([In] uint celt);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

	new void GetCount(out uint pcelt);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([In] uint celt, [Out][MarshalAs(UnmanagedType.LPArray)] ICorDebugAssembly[] values, out uint pceltFetched);
}
