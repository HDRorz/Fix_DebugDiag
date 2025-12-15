using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("35DE7FD4-902C-4CC5-9C87-A7C9632B4B4A")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDReferenceEnum
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetCount(out int pCount);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void Reset();

	[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	int Next(int count, [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] MD_Reference[] refs, out int pWrote);
}
