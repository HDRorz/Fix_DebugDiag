namespace Microsoft.Diagnostics.Runtime.Interop;

public enum DEBUG_DUMP : uint
{
	SMALL = 1024u,
	DEFAULT = 1025u,
	FULL = 1026u,
	IMAGE_FILE = 1027u,
	TRACE_LOG = 1028u,
	WINDOWS_CD = 1029u,
	KERNEL_DUMP = 1025u,
	KERNEL_SMALL_DUMP = 1024u,
	KERNEL_FULL_DUMP = 1026u
}
