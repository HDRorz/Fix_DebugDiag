namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct HandleData
{
	public ulong AppDomain;

	public ulong Handle;

	public ulong Secondary;

	public uint Type;

	public uint StrongReference;

	public uint RefCount;

	public uint JupiterRefCount;

	public uint IsPegged;
}
