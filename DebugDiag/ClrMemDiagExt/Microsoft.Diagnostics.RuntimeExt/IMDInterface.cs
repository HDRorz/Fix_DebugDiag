using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("31980D20-963F-11E1-A8B0-0800200C9A66")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDInterface
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetName([MarshalAs(UnmanagedType.BStr)] out string pName);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetBaseInterface([MarshalAs(UnmanagedType.Interface)] out IMDInterface ppBase);
}
