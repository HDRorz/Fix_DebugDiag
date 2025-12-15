namespace Microsoft.Diagnostics.Runtime.Linux;

internal interface IElfPRStatus
{
	uint ProcessId { get; }

	uint ThreadId { get; }

	unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context);
}
