namespace DebugDiag.DbgEng;

public enum DEBUG_SYMTYPE : uint
{
	NONE,
	COFF,
	CODEVIEW,
	PDB,
	EXPORT,
	DEFERRED,
	SYM,
	DIA
}
