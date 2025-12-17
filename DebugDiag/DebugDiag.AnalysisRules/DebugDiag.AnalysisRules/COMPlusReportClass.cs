namespace DebugDiag.AnalysisRules;

internal class COMPlusReportClass
{
	public bool IsError;

	public bool IsWarning;

	public string Description = "";

	public int Weight;

	public double PercentThreadsBusy;

	public bool EmulateMTSBehaviorIsSet;

	public int InstanceCount;
}
