using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_GET_TEXT_COMPLETIONS : uint
{
	NONE = 0u,
	NO_DOT_COMMANDS = 1u,
	NO_EXTENSION_COMMANDS = 2u,
	NO_SYMBOLS = 4u,
	IS_DOT_COMMAND = 1u,
	IS_EXTENSION_COMMAND = 2u,
	IS_SYMBOL = 4u
}
