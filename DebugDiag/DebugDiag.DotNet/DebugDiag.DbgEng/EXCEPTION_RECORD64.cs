namespace DebugDiag.DbgEng;

public struct EXCEPTION_RECORD64
{
	public uint ExceptionCode;

	public uint ExceptionFlags;

	public ulong ExceptionRecord;

	public ulong ExceptionAddress;

	public uint NumberParameters;

	public uint __unusedAlignment;

	public unsafe fixed ulong ExceptionInformation[15];
}
