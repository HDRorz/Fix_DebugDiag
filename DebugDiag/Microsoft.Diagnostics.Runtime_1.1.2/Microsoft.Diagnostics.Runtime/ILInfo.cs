namespace Microsoft.Diagnostics.Runtime;

public class ILInfo
{
	public ulong Address { get; internal set; }

	public int Length { get; internal set; }

	public int MaxStack { get; internal set; }

	public uint Flags { get; internal set; }

	public uint LocalVarSignatureToken { get; internal set; }
}
