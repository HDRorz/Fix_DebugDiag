using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

[ComImport]
[Guid("1C07BA58-6A4F-4E44-90F2-A62056F379B9")]
[CoClass(typeof(ScriptManagerClass))]
public interface ScriptManager : IManager2, IManager
{
}
