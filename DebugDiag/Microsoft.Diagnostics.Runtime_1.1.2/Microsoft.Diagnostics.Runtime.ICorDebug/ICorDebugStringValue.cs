using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAFD-8A68-11D2-983C-0000F808342D")]
[ComConversionLoss]
[InterfaceType(1)]
public interface ICorDebugStringValue : ICorDebugHeapValue, ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	new void IsValid(out int pbValid);

	new void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void GetLength(out uint pcchString);

	void GetString([In] uint cchString, out uint pcchString, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szString);
}
