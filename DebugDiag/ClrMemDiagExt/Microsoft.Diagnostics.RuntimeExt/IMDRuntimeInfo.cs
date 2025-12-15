using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComImport]
[Guid("4AADFA25-0486-48EB-9338-D4B39E23AF82")]
[TypeLibType(TypeLibTypeFlags.FNonExtensible)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMDRuntimeInfo
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetRuntimeVersion([MarshalAs(UnmanagedType.BStr)] out string pVersion);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetDacLocation([MarshalAs(UnmanagedType.BStr)] out string pVersion);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetDacRequestData(out int pTimestamp, out int pFilesize);

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	void GetDacRequestFilename([MarshalAs(UnmanagedType.BStr)] out string pRequestFileName);
}
