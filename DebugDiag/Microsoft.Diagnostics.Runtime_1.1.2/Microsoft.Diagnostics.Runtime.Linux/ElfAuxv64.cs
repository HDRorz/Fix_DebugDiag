using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfAuxv64
{
	public readonly ulong Type;

	public readonly ulong Value;
}
