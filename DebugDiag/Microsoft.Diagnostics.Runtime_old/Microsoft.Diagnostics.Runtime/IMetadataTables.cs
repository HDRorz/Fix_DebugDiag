using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[Guid("D8F579AB-402D-4b8e-82D9-5D63B1065C68")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMetadataTables
{
	void GetStringHeapSize(out uint countBytesStrings);

	void GetBlobHeapSize(out uint countBytesBlobs);

	void GetGuidHeapSize(out uint countBytesGuids);

	void GetUserStringHeapSize(out uint countByteBlobs);

	void GetNumTables(out uint countTables);

	void GetTableIndex(uint token, out uint tableIndex);

	void GetTableInfo(uint tableIndex, out uint countByteRows, out uint countRows, out uint countColumns, out uint columnPrimaryKey, [MarshalAs(UnmanagedType.LPStr)] out string name);
}
