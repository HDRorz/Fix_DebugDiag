using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum EFileAccess : uint
{
	None = 0u,
	GenericRead = 0x80000000u,
	GenericWrite = 0x40000000u,
	GenericExecute = 0x20000000u,
	GenericAll = 0x10000000u
}
