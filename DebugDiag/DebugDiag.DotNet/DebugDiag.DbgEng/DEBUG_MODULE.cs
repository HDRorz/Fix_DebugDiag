using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum DEBUG_MODULE : uint
{
	LOADED = 0u,
	UNLOADED = 1u,
	USER_MODE = 2u,
	EXE_MODULE = 4u,
	EXPLICIT = 8u,
	SECONDARY = 0x10u,
	SYNTHETIC = 0x20u,
	SYM_BAD_CHECKSUM = 0x10000u
}
