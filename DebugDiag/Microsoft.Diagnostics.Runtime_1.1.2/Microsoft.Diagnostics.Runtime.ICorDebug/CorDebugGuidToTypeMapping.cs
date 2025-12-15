using System;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct CorDebugGuidToTypeMapping
{
	public Guid iid;

	public ICorDebugType icdType;
}
