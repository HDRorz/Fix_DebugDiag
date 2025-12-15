using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("C31AD976-24BF-4F4C-85B0-7F18484C2600")]
[CoClass(typeof(DbgObjClass_Legacy))]
internal interface DbgObj_Legacy : IDbgObj4
{
}
