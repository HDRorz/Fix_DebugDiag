namespace DebugDiag.DbgEng;

public enum CODE_PAGE : uint
{
	ACP = 0u,
	OEMCP = 1u,
	MACCP = 2u,
	THREAD_ACP = 3u,
	SYMBOL = 42u,
	UTF7 = 65000u,
	UTF8 = 65001u
}
