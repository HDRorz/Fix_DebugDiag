using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_CONNECT_SESSION : uint
{
	DEFAULT = 0u,
	NO_VERSION = 1u,
	NO_ANNOUNCE = 2u
}
