using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("BC6675B0-30AA-4217-9F84-2F4CD2A7BE96")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IHeapInfo
{
}
