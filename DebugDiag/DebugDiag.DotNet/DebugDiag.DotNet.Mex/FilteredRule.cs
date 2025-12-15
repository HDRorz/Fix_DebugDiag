using System;

namespace DebugDiag.DotNet.Mex;

internal class FilteredRule : MexWrapperBase
{
	public UdeRuleAttribute RuleInfo
	{
		get
		{
			return new UdeRuleAttribute(RealMexObject.RuleInfo);
		}
		set
		{
			RealMexObject.RuleInfo = value.RealMexObject;
		}
	}

	public bool RunRule
	{
		get
		{
			return RealMexObject.RunRule;
		}
		set
		{
			RealMexObject.RunRule = value;
		}
	}

	public Type Type
	{
		get
		{
			return RealMexObject.Type;
		}
		set
		{
			RealMexObject.Type = value;
		}
	}

	public string FilterDetails
	{
		get
		{
			return RealMexObject.FilterDetails;
		}
		set
		{
			RealMexObject.FilterDetails = value;
		}
	}

	public FilteredRule(object realMexObject)
		: base(realMexObject)
	{
	}
}
