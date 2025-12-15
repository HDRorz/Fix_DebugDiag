namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct WorkRequestData
{
	public readonly ulong Function;

	public readonly ulong Context;

	public readonly ulong NextWorkRequest;
}
