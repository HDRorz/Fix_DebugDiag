using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[ComConversionLoss]
[Guid("CC7BCB06-8A68-11D2-983C-0000F808342D")]
public interface ICorDebugThreadEnum : ICorDebugEnum
{
	new void Skip([In] uint celt);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

	new void GetCount(out uint pcelt);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([In] uint celt, [Out][MarshalAs(UnmanagedType.LPArray)] ICorDebugThread[] threads, out uint pceltFetched);
}
