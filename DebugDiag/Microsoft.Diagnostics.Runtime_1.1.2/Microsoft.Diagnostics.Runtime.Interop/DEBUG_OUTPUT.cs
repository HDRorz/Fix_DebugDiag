using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_OUTPUT : uint
{
	NORMAL = 1u,
	ERROR = 2u,
	WARNING = 4u,
	VERBOSE = 8u,
	PROMPT = 0x10u,
	PROMPT_REGISTERS = 0x20u,
	EXTENSION_WARNING = 0x40u,
	DEBUGGEE = 0x80u,
	DEBUGGEE_PROMPT = 0x100u,
	SYMBOLS = 0x200u
}
