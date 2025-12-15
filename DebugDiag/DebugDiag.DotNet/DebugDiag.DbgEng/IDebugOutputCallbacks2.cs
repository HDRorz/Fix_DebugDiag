using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("67721fe9-56d2-4a44-a325-2b65513ce6eb")]
public interface IDebugOutputCallbacks2 : IDebugOutputCallbacks
{
	[PreserveSig]
	new int Output([In] DEBUG_OUTPUT Mask, [In][MarshalAs(UnmanagedType.LPStr)] string Text);

	[PreserveSig]
	int GetInterestMask(out DEBUG_OUTCBI Mask);

	[PreserveSig]
	int Output2([In] DEBUG_OUTCB Which, [In] DEBUG_OUTCBF Flags, [In] ulong Arg, [In][MarshalAs(UnmanagedType.LPWStr)] string Text);
}
