using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[StructLayout(LayoutKind.Sequential)]
internal class MINIDUMP_SYSTEM_INFO
{
	public ProcessorArchitecture ProcessorArchitecture;

	public ushort ProcessorLevel;

	public ushort ProcessorRevision;

	public byte NumberOfProcessors;

	public byte ProductType;

	public uint MajorVersion;

	public uint MinorVersion;

	public uint BuildNumber;

	public int PlatformId;

	public RVA CSDVersionRva;

	public Version Version => new Version((int)MajorVersion, (int)MinorVersion, (int)BuildNumber);
}
