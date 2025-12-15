namespace Microsoft.Diagnostics.Runtime.Native;

internal struct NativeCodeHeader
{
	public ulong GCInfo;

	public ulong EHInfo;

	public ulong MethodStart;

	public uint MethodSize;
}
