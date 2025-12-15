namespace Microsoft.Diagnostics.Runtime.Interop;

public enum ECreationDisposition : uint
{
	New = 1u,
	CreateAlways,
	OpenExisting,
	OpenAlways,
	TruncateExisting
}
