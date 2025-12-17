using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IISInfoLib;

[ComImport]
[CompilerGenerated]
[DefaultMember("Item")]
[Guid("2801B39E-506B-4984-9833-8F07545B7850")]
[TypeIdentifier]
public interface IASPApps : IEnumerable
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
