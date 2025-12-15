using System;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[Flags]
public enum CorDebugMDAFlags
{
	None = 0,
	MDA_FLAG_SLIP = 2
}
