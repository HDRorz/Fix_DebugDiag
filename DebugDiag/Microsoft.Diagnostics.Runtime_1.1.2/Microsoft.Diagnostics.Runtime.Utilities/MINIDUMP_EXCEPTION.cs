using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[StructLayout(LayoutKind.Sequential)]
internal class MINIDUMP_EXCEPTION
{
	public uint ExceptionCode;

	public uint ExceptionFlags;

	public ulong ExceptionRecord;

	private ulong _exceptionaddress;

	public uint NumberParameters;

	public uint __unusedAlignment;

	public ulong[] ExceptionInformation;

	public ulong ExceptionAddress
	{
		get
		{
			return DumpNative.ZeroExtendAddress(_exceptionaddress);
		}
		set
		{
			_exceptionaddress = value;
		}
	}

	public MINIDUMP_EXCEPTION()
	{
		ExceptionInformation = new ulong[15];
	}
}
