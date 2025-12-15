using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[Guid("CC7BCAE9-8A68-11D2-983C-0000F808342D")]
[InterfaceType(1)]
public interface ICorDebugFunctionBreakpoint : ICorDebugBreakpoint
{
	new void Activate([In] int bActive);

	new void IsActive(out int pbActive);

	void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetOffset(out uint pnOffset);
}
