using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("FA20085F-8892-4EAA-9F9A-112A8A4C5F69")]
public interface IASPInfo
{
}
