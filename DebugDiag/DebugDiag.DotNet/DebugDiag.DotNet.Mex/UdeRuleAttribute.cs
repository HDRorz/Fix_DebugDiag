namespace DebugDiag.DotNet.Mex;

internal class UdeRuleAttribute : MexWrapperBase
{
	public bool DisableUDE
	{
		get
		{
			return RealMexObject.DisableUDE;
		}
		set
		{
			RealMexObject.DisableUDE = value;
		}
	}

	public string Description
	{
		get
		{
			return RealMexObject.Description;
		}
		set
		{
			RealMexObject.Description = value;
		}
	}

	public UdeRuleAttribute(object realMexObject)
		: base(realMexObject)
	{
	}
}
