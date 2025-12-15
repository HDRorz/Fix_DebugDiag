using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCB03-8A68-11D2-983C-0000F808342D")]
[ComConversionLoss]
[InterfaceType(1)]
public interface ICorDebugBreakpointEnum : ICorDebugEnum
{
	new void Skip([In] uint celt);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

	new void GetCount(out uint pcelt);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([In] uint celt, [Out][MarshalAs(UnmanagedType.LPArray)] ICorDebugBreakpoint[] breakpoints, out uint pceltFetched);
}
