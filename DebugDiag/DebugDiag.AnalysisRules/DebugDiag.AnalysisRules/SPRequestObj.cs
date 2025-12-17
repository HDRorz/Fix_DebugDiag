namespace DebugDiag.AnalysisRules;

public class SPRequestObj
{
	public SPRequestType Type { get; set; }

	public ulong Address { get; set; }

	public ulong SPRequest { get; set; }

	public int ThreadID { get; set; }

	public SPRequestObj(SPRequestType SPType, ulong Obj, ulong Request, int Thread)
	{
		Type = SPType;
		Address = Obj;
		SPRequest = Request;
		ThreadID = Thread;
	}
}
