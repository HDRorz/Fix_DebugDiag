using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DebugDiag.DotNet.Mex;
using Microsoft.Mex.DotNetDbg;
using Microsoft.Mex.Framework;

namespace DebugDiag.DotNet.AnalysisRules;

[DataContract]
internal class MexCodeAnalysisRuleInfo : CodeAnalysisRuleInfoBase
{
	public RuleFilterResults FilterResult;

	private static readonly string TempDir = Environment.ExpandEnvironmentVariables("%TEMP%");

	private static DebugUtilities _d;

	private string _description;

	private object[] _customAttributes;

	private UdeScanRule _udeScanRuleInstance;

	private UdeRuleAttribute _ruleInfo;

	private static MethodInfo _getUdeScanDataMethodInfo;

	private static MethodInfo _getFilteredRulesMethodInfo;

	private static UdeScanData ScanData;

	public static FilteredRules FilteredRules;

	private static bool haveMexRules = false;

	public UdeRuleAttribute RuleInfo => _ruleInfo;

	public UdeScanRule UdeScanRuleInstance => _udeScanRuleInstance;

	public override string Category => "MEX Rules";

	public override string Description => _description;

	public static void InitRuleEngine(Assembly mexRuleEngineAssembly)
	{
		if (_getUdeScanDataMethodInfo == null)
		{
			haveMexRules = true;
			Type type = mexRuleEngineAssembly.GetType("Microsoft.Mex.MexRuleEngine.RuleEngine");
			_getUdeScanDataMethodInfo = type.GetMethod("GetUdeScanData", new Type[1] { typeof(DebugUtilities) });
			if (_getUdeScanDataMethodInfo == null)
			{
				throw new Exception("Couldn't load _getUdeScanDataMethodInfo");
			}
			_getFilteredRulesMethodInfo = type.GetMethod("GetFilteredRules");
			if (_getFilteredRulesMethodInfo == null)
			{
				throw new Exception("Couldn't load _getFilteredRulesMethodInfo");
			}
		}
	}

	protected override void Init(Type analysisRuleType, string originalLocation)
	{
		haveMexRules = true;
		base.Init(analysisRuleType, originalLocation);
		_customAttributes = analysisRuleType.GetCustomAttributes(inherit: true);
		_ruleInfo = new UdeRuleAttribute(_customAttributes.First((object a) => a.GetType().Name == "UdeRuleAttribute"));
		_description = _ruleInfo.Description;
	}

	public static void Reset(NetDbgObj debugger)
	{
		if (haveMexRules)
		{
			_d = new DebugUtilities((IDebugClient)debugger.RawDebugger)
			{
				IsFirstCommand = true
			};
			MexFrameworkClass.Initialize(_d, TempDir);
			ParameterInfo[] parameters = _getUdeScanDataMethodInfo.GetParameters();
			int newSize = ((parameters != null) ? parameters.Length : 0);
			object[] array = new object[1] { _d };
			Array.Resize(ref array, newSize);
			ScanData = new UdeScanData(_getUdeScanDataMethodInfo.Invoke(null, array));
			parameters = _getFilteredRulesMethodInfo.GetParameters();
			newSize = ((parameters != null) ? parameters.Length : 0);
			array = new object[2] { _d, ScanData.RealMexObject };
			Array.Resize(ref array, newSize);
			FilteredRules = new FilteredRules(_getFilteredRulesMethodInfo.Invoke(null, array));
		}
	}

	public void Reset()
	{
		FilteredRule filteredRule = FilteredRules[base.AnalysisRuleType];
		FilterResult = ((!filteredRule.RunRule) ? RuleFilterResults.FilterReturnedFalse : RuleFilterResults.FilterReturnedTrue);
		FilterReason = filteredRule.FilterDetails;
		ScanData.RuleInfo = _ruleInfo;
		object obj = Activator.CreateInstance(_analysisRuleType);
		_analysisRuleType.GetMethod("InternalConstructor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod).Invoke(obj, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2] { _d, ScanData.RealMexObject }, null);
		_udeScanRuleInstance = new UdeScanRule(obj);
	}

	private static T Cast<T>(object o)
	{
		return (T)o;
	}

	public MexCodeAnalysisRuleInfo(Type analysisRuleType, string originalLocation)
		: base(analysisRuleType, originalLocation)
	{
	}
}
