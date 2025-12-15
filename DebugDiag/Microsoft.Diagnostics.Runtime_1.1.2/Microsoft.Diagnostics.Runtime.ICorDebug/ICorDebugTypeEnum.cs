using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("10F27499-9DF2-43CE-8333-A321D7C99CB4")]
[InterfaceType(1)]
[ComConversionLoss]
public interface ICorDebugTypeEnum : ICorDebugEnum
{
	new void Skip([In] uint celt);

	new void Reset();

	new void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

	new void GetCount(out uint pcelt);

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next([In] uint celt, [Out][MarshalAs(UnmanagedType.LPArray)] ICorDebugType[] values, out uint pceltFetched);
}
