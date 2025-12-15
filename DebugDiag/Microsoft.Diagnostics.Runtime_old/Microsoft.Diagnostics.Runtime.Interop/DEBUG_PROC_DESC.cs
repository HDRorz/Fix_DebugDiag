using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_PROC_DESC : uint
{
	DEFAULT = 0u,
	NO_PATHS = 1u,
	NO_SERVICES = 2u,
	NO_MTS_PACKAGES = 4u,
	NO_COMMAND_LINE = 8u,
	NO_SESSION_ID = 0x10u,
	NO_USER_NAME = 0x20u
}
