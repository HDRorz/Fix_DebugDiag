using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class MinidumpArray<T>
{
	private DumpPointer _streamPointer;

	public uint Count => _streamPointer.ReadUInt32();

	protected MinidumpArray(DumpPointer streamPointer, MINIDUMP_STREAM_TYPE streamType)
	{
		if (streamType != MINIDUMP_STREAM_TYPE.ModuleListStream && streamType != MINIDUMP_STREAM_TYPE.ThreadListStream && streamType != MINIDUMP_STREAM_TYPE.ThreadExListStream)
		{
			throw new ClrDiagnosticsException("MinidumpArray does not support this stream type.", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		_streamPointer = streamPointer;
	}

	public T GetElement(uint idx)
	{
		if (idx > Count)
		{
			throw new ClrDiagnosticsException("Dump error: index " + idx + "is out of range.", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		uint offset = (uint)(4 + (int)idx * Marshal.SizeOf(typeof(T)));
		return _streamPointer.PtrToStructure<T>(offset);
	}
}
