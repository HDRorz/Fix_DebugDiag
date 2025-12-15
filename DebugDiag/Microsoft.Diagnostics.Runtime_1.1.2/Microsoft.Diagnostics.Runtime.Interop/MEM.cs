using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

[Flags]
public enum MEM : uint
{
	COMMIT = 0x1000u,
	RESERVE = 0x2000u,
	DECOMMIT = 0x4000u,
	RELEASE = 0x8000u,
	FREE = 0x10000u,
	PRIVATE = 0x20000u,
	MAPPED = 0x40000u,
	RESET = 0x80000u,
	TOP_DOWN = 0x100000u,
	WRITE_WATCH = 0x200000u,
	PHYSICAL = 0x400000u,
	ROTATE = 0x800000u,
	LARGE_PAGES = 0x20000000u,
	FOURMB_PAGES = 0x80000000u,
	IMAGE = 0x1000000u
}
