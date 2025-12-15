using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("3DCFD00B-C03C-4009-9034-9370908EB78D")]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
public interface IHTTPInfo
{
}
