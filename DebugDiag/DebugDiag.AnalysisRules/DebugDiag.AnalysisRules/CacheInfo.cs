using System.Collections.Generic;

namespace DebugDiag.AnalysisRules;

internal class CacheInfo
{
	public List<ulong> Cache = new List<ulong>();

	public ulong Size;

	public ulong SizeSquared;

	public ulong MaxSize;

	public ulong MinSize = ulong.MaxValue;
}
