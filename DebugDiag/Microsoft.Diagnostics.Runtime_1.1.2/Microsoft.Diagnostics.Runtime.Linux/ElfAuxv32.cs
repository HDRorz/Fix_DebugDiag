using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfAuxv32
{
	public readonly uint Type;

	public readonly uint Value;
}
