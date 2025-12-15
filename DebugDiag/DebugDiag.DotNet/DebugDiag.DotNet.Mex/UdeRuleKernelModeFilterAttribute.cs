namespace DebugDiag.DotNet.Mex;

internal class UdeRuleKernelModeFilterAttribute : MexWrapperBase
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

	public bool Manual
	{
		get
		{
			return RealMexObject.Manual;
		}
		set
		{
			RealMexObject.Manual = value;
		}
	}

	public uint BugCheckCode
	{
		get
		{
			return RealMexObject.BugCheckCode;
		}
		set
		{
			RealMexObject.BugCheckCode = value;
		}
	}

	public UdeRuleKernelModeFilterAttribute(object realMexObject)
		: base(realMexObject)
	{
	}
}
