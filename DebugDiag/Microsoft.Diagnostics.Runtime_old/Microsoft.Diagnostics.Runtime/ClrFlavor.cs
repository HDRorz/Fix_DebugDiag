using System;

namespace Microsoft.Diagnostics.Runtime;

public enum ClrFlavor
{
	Desktop = 0,
	CoreCLR = 1,
	[Obsolete("Use Native instead.")]
	Redhawk = 2,
	Native = 2
}
