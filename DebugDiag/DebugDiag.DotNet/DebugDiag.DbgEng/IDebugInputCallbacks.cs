using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9f50e42c-f136-499e-9a97-73036c94ed2d")]
public interface IDebugInputCallbacks
{
	[PreserveSig]
	int StartInput([In] uint BufferSize);

	[PreserveSig]
	int EndInput();
}
