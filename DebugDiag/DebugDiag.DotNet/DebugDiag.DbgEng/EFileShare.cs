using System;

namespace DebugDiag.DbgEng;

[Flags]
public enum EFileShare : uint
{
	None = 0u,
	Read = 1u,
	Write = 2u,
	Delete = 4u
}
