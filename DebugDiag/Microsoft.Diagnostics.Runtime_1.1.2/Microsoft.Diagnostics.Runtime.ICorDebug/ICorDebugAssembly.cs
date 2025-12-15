using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[Guid("DF59507C-D47A-459E-BCE2-6427EAC8FD06")]
[InterfaceType(1)]
public interface ICorDebugAssembly
{
	void GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

	void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);

	void EnumerateModules([MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleEnum ppModules);

	void GetCodeBase([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);

	void GetName([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);
}
