namespace DebugDiag.DotNet.Mex;

internal class UdeScanData : MexWrapperBase
{
	public UdeRuleAttribute RuleInfo
	{
		get
		{
			return RealMexObject.RuleInfo;
		}
		set
		{
			RealMexObject.RuleInfo = value.RealMexObject;
		}
	}

	public bool UdeMode
	{
		get
		{
			return RealMexObject.UdeMode;
		}
		set
		{
			RealMexObject.UdeMode = value;
		}
	}

	public BugCheckData BugCheckData => new BugCheckData(RealMexObject.BugCheckData);

	public UdeScanData(object realMexObject)
		: base(realMexObject)
	{
	}
}
