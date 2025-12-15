namespace DebugDiag.DotNet.Mex;

internal class UdeRuleUserModeFilterAttribute : MexWrapperBase
{
	public bool All
	{
		get
		{
			return RealMexObject.All;
		}
		set
		{
			RealMexObject.All = value;
		}
	}

	public string ProcessName
	{
		get
		{
			return RealMexObject.ProcessName;
		}
		set
		{
			RealMexObject.ProcessName = value;
		}
	}

	public string ModuleName
	{
		get
		{
			return RealMexObject.ModuleName;
		}
		set
		{
			RealMexObject.ModuleName = value;
		}
	}

	public UdeRuleUserModeFilterAttribute(object realMexObject)
		: base(realMexObject)
	{
	}
}
