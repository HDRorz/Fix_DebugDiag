namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct MINIDUMP_HEADER
{
	public readonly uint Singature;

	public readonly uint Version;

	public readonly uint NumberOfStreams;

	public readonly uint StreamDirectoryRva;

	public readonly uint CheckSum;

	public readonly uint TimeDateStamp;

	public readonly ulong Flags;
}
