using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum DEBUG_ASMOPT : uint
{
	DEFAULT = 0u,
	VERBOSE = 1u,
	NO_CODE_BYTES = 2u,
	IGNORE_OUTPUT_WIDTH = 4u,
	SOURCE_LINE_NUMBER = 8u
}
