using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComplusDDExt;

[ComImport]
[CompilerGenerated]
[Guid("505B5200-344B-405C-8AE7-C7DCB9901702")]
[TypeIdentifier]
public interface IComplusRoot
{
	[DispId(1)]
	CBlockingActivityInfo BlockingActivityInfo
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}
}
