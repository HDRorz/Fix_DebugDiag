using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfSignalInfo
{
	public int Number;

	public int Code;

	public int Errno;
}
