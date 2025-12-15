using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum MODULE_ORDERS : uint
{
	MASK = 0xF0000000u,
	LOADTIME = 0x10000000u,
	MODULENAME = 0x20000000u
}
