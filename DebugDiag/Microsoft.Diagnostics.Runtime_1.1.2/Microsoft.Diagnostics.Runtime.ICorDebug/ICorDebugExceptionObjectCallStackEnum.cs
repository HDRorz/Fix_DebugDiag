using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[InterfaceType(1)]
[Guid("ED775530-4DC4-41F7-86D0-9E2DEF7DFC66")]
public interface ICorDebugExceptionObjectCallStackEnum : ICorDebugEnum
{
	new void Skip([In] uint celt);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

	new void GetCount(out uint pcelt);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([In] uint celt, [Out][MarshalAs(UnmanagedType.LPArray)] CorDebugExceptionObjectStackFrame[] values, out uint pceltFetched);
}
