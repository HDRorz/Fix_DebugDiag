using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4bf58045-d654-4c40-b0af-683090f356dc")]
public interface IDebugOutputCallbacks
{
	[PreserveSig]
	int Output([In] DEBUG_OUTPUT Mask, [In][MarshalAs(UnmanagedType.LPStr)] string Text);
}
