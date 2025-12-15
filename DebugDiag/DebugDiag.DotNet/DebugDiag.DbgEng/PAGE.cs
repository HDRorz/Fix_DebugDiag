using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum PAGE : uint
{
	NOACCESS = 1u,
	READONLY = 2u,
	READWRITE = 4u,
	WRITECOPY = 8u,
	EXECUTE = 0x10u,
	EXECUTE_READ = 0x20u,
	EXECUTE_READWRITE = 0x40u,
	EXECUTE_WRITECOPY = 0x80u,
	GUARD = 0x100u,
	NOCACHE = 0x200u,
	WRITECOMBINE = 0x400u
}
