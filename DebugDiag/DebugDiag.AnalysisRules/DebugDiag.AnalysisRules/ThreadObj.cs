namespace DebugDiag.AnalysisRules;

public class ThreadObj
{
	public string TypeName { get; internal set; }

	public ulong Address { get; internal set; }

	public bool IsAlive { get; internal set; }

	public uint OSThreadId { get; internal set; }

	public bool IsPossibleFalsePositive { get; internal set; }
}
