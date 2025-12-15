using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[InterfaceType(1)]
[Guid("6DC3FA01-D7CB-11D2-8A95-0080C792E5D8")]
public interface ICorDebugEditAndContinueSnapshot
{
	void CopyMetaData([In][MarshalAs(UnmanagedType.Interface)] IStream pIStream, out Guid pMvid);

	void GetMvid(out Guid pMvid);

	void GetRoDataRVA(out uint pRoDataRVA);

	void GetRwDataRVA(out uint pRwDataRVA);

	void SetPEBytes([In][MarshalAs(UnmanagedType.Interface)] IStream pIStream);

	void SetILMap([In] uint mdFunction, [In] uint cMapSize, [In] ref COR_IL_MAP map);

	void SetPESymbolBytes([In][MarshalAs(UnmanagedType.Interface)] IStream pIStream);
}
