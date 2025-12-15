using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum EFileShare : uint
{
	None = 0u,
	Read = 1u,
	Write = 2u,
	Delete = 4u
}
