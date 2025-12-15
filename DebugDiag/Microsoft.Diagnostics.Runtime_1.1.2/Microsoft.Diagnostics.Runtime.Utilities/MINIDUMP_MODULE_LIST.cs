namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class MINIDUMP_MODULE_LIST : MinidumpArray<MINIDUMP_MODULE>
{
	internal MINIDUMP_MODULE_LIST(DumpPointer streamPointer)
		: base(streamPointer, MINIDUMP_STREAM_TYPE.ModuleListStream)
	{
	}
}
