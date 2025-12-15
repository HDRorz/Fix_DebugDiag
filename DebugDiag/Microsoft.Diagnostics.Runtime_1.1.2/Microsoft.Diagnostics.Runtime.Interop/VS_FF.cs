using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum VS_FF : uint
{
	DEBUG = 1u,
	PRERELEASE = 2u,
	PATCHED = 4u,
	PRIVATEBUILD = 8u,
	INFOINFERRED = 0x10u,
	SPECIALBUILD = 0x20u
}
