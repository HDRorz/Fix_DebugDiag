using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("429DB77C-C4C7-4A3C-8699-817EA88903E0")]
[CoClass(typeof(DbgControlClass_Legacy))]
public interface DbgControl_Legacy : IDbgControl
{
}
