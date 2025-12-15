using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[Guid("0405B0DF-A660-11D2-BD02-0000F80849BD")]
[InterfaceType(1)]
public interface ICorDebugArrayValue : ICorDebugHeapValue, ICorDebugValue
{
	new void GetType(out CorElementType pType);

	new void GetSize(out uint pSize);

	new void GetAddress(out ulong pAddress);

	new void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	new void IsValid(out int pbValid);

	new void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);

	void GetElementType(out CorElementType pType);

	void GetRank(out uint pnRank);

	void GetCount(out uint pnCount);

	void GetDimensions([In] uint cdim, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] dims);

	void HasBaseIndicies(out int pbHasBaseIndicies);

	void GetBaseIndicies([In] uint cdim, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] indicies);

	void GetElement([In] uint cdim, [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] indices, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetElementAtPosition([In] uint nPosition, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
}
