using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[StructLayout(LayoutKind.Sequential)]
internal class MINIDUMP_EXCEPTION_STREAM
{
	public uint ThreadId;

	public uint __alignment;

	public MINIDUMP_EXCEPTION ExceptionRecord;

	public MINIDUMP_LOCATION_DESCRIPTOR ThreadContext;

	public MINIDUMP_EXCEPTION_STREAM(DumpPointer dump)
	{
		uint offset = 0u;
		ThreadId = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		__alignment = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		ExceptionRecord = new MINIDUMP_EXCEPTION();
		ExceptionRecord.ExceptionCode = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		ExceptionRecord.ExceptionFlags = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		ExceptionRecord.ExceptionRecord = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
		ExceptionRecord.ExceptionAddress = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
		ExceptionRecord.NumberParameters = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		ExceptionRecord.__unusedAlignment = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		if ((long)ExceptionRecord.ExceptionInformation.Length != 15)
		{
			throw new ClrDiagnosticsException("Crash dump error: Expected to find " + 15u + " exception params, but found " + ExceptionRecord.ExceptionInformation.Length + " instead.", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		for (int i = 0; (long)i < 15L; i++)
		{
			ExceptionRecord.ExceptionInformation[i] = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
		}
		ThreadContext.DataSize = dump.PtrToStructureAdjustOffset<uint>(ref offset);
		ThreadContext.Rva.Value = dump.PtrToStructureAdjustOffset<uint>(ref offset);
	}
}
