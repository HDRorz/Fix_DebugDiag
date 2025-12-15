using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum SEC : uint
{
	FILE = 0x800000u,
	IMAGE = 0x1000000u,
	PROTECTED_IMAGE = 0x2000000u,
	RESERVE = 0x4000000u,
	COMMIT = 0x8000000u,
	NOCACHE = 0x10000000u,
	WRITECOMBINE = 0x40000000u,
	LARGE_PAGES = 0x80000000u,
	MEM_IMAGE = 0x1000000u
}
