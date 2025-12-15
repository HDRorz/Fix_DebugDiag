namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct HandleData
{
	public readonly ulong AppDomain;

	public readonly ulong Handle;

	public readonly ulong Secondary;

	public readonly uint Type;

	public readonly uint StrongReference;

	public readonly uint RefCount;

	public readonly uint JupiterRefCount;

	public readonly uint IsPegged;
}
