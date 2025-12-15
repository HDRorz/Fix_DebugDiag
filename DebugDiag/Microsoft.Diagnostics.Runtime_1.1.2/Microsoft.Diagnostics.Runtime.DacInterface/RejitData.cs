namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct RejitData
{
	private readonly ulong RejitID;

	private readonly uint Flags;

	private readonly ulong NativeCodeAddr;
}
