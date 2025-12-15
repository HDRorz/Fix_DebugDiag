using System.Runtime.InteropServices;

namespace DebugDiag.DotNet.DbgLibInterop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("7E08B926-A20A-4090-91AD-5B9E78F1F948")]
public interface IDbgCOMExt
{
	void InitExtension([MarshalAs(UnmanagedType.IUnknown)] object pDebugClient, [MarshalAs(UnmanagedType.IUnknown)] object pDbgCOM);

	void TerminateExtension();
}
