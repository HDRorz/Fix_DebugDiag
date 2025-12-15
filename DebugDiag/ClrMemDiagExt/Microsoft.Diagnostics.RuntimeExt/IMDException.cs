using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[Guid("E3F30A85-0DBB-44C3-AA3E-460CCF31A2F1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDException
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetGCHeapType([MarshalAs(UnmanagedType.Interface)] out IMDType ppType);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetMessage([MarshalAs(UnmanagedType.BStr)] out string pMessage);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetObjectAddress(out ulong pAddress);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetInnerException([MarshalAs(UnmanagedType.Interface)] out IMDException ppException);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetHRESULT([MarshalAs(UnmanagedType.Error)] out int pHResult);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void EnumerateStackFrames([MarshalAs(UnmanagedType.Interface)] out IMDStackTraceEnum ppEnum);
}
