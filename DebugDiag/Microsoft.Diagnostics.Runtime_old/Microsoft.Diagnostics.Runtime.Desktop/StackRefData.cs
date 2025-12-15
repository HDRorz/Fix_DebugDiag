namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct StackRefData
{
	public uint HasRegisterInformation;

	public int Register;

	public int Offset;

	public ulong Address;

	public ulong Object;

	public uint Flags;

	public uint SourceType;

	public ulong Source;

	public ulong StackPointer;
}
