using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IISInfoLib;

[ComImport]
[CompilerGenerated]
[Guid("17E57869-70CA-4195-83FA-9B20909BDF12")]
[DefaultMember("Item")]
[TypeIdentifier]
public interface IASPCurrentRequests : IEnumerable
{
	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	void _VtblGap1_1();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(-4)]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	new IEnumerator GetEnumerator();
}
