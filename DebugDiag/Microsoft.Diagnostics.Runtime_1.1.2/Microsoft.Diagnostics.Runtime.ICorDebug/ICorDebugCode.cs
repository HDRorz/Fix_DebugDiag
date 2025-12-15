using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("CC7BCAF4-8A68-11D2-983C-0000F808342D")]
[ComConversionLoss]
public interface ICorDebugCode
{
	void IsIL(out int pbIL);

	void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetAddress(out ulong pStart);

	void GetSize(out uint pcBytes);

	void CreateBreakpoint([In] uint offset, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);

	void GetCode([In] uint startOffset, [In] uint endOffset, [In] uint cBufferAlloc, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] buffer, out uint pcBufferSize);

	void GetVersionNumber(out uint nVersion);

	void GetILToNativeMapping([In] uint cMap, out uint pcMap, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] COR_DEBUG_IL_TO_NATIVE_MAP[] map);

	void GetEnCRemapSequencePoints([In] uint cMap, out uint pcMap, [Out][MarshalAs(UnmanagedType.LPArray)] uint[] offsets);
}
