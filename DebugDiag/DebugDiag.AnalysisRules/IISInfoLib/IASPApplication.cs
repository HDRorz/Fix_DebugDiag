using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IISInfoLib;

[ComImport]
[CompilerGenerated]
[Guid("00CD12E6-37E1-4AA8-BB61-17B2172B8EEA")]
[DefaultMember("Item")]
[TypeIdentifier]
public interface IASPApplication : IEnumerable
{
	[DispId(1)]
	string MetabaseKey
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	void _VtblGap1_16();

	[DispId(18)]
	bool AllowDebugging
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(18)]
		get;
	}
}
