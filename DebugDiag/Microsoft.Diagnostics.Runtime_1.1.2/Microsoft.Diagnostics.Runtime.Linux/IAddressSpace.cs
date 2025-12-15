namespace Microsoft.Diagnostics.Runtime.Linux;

internal interface IAddressSpace
{
	long Length { get; }

	string Name { get; }

	int Read(long position, byte[] buffer, int bufferOffset, int count);
}
