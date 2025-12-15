using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_OUTCTL : uint
{
	THIS_CLIENT = 0u,
	ALL_CLIENTS = 1u,
	ALL_OTHER_CLIENTS = 2u,
	IGNORE = 3u,
	LOG_ONLY = 4u,
	SEND_MASK = 7u,
	NOT_LOGGED = 8u,
	OVERRIDE_MASK = 0x10u,
	DML = 0x20u,
	AMBIENT_DML = 0xFFFFFFFEu,
	AMBIENT_TEXT = uint.MaxValue
}
