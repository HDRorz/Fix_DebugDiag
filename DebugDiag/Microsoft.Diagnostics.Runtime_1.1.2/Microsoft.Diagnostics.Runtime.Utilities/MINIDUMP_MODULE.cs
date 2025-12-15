using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal sealed class MINIDUMP_MODULE
{
	private ulong _baseofimage;

	public uint SizeOfImage;

	public uint CheckSum;

	public uint TimeDateStamp;

	public RVA ModuleNameRva;

	internal VS_FIXEDFILEINFO VersionInfo;

	private MINIDUMP_LOCATION_DESCRIPTOR _cvRecord;

	private MINIDUMP_LOCATION_DESCRIPTOR _miscRecord;

	private ulong _reserved0;

	private ulong _reserved1;

	public ulong BaseOfImage => DumpNative.ZeroExtendAddress(_baseofimage);

	public DateTime Timestamp => DateTime.FromFileTimeUtc(10000000L * (long)TimeDateStamp + 116444736000000000L);
}
