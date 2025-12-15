using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace DebugDiag.DotNet.AnalysisRules;

[DataContract]
public abstract class CodeAnalysisRuleInfoBase : AnalysisRuleInfo
{
	internal Type _analysisRuleType;

	public Type AnalysisRuleType => _analysisRuleType;

	public override string DisplayName => _analysisRuleType.Name;

	[DataMember]
	internal override string SerializedTypeInfo
	{
		get
		{
			return $"{_analysisRuleType.Assembly.Location};{_analysisRuleType.FullName}";
		}
		set
		{
			string[] array = value.Split(';');
			if (array.Length != 2)
			{
				throw new Exception($"Invalid SerializedTypeInfo: {value}");
			}
			string assemblyFile = array[0];
			string name = array[1];
			Assembly assembly = Assembly.LoadFrom(assemblyFile);
			MexCodeAnalysisRuleInfo.InitRuleEngine(assembly);
			Init(assembly.GetType(name, throwOnError: true), null);
		}
	}

	public CodeAnalysisRuleInfoBase(Type analysisRuleType, string originalLocation)
	{
		Init(analysisRuleType, originalLocation);
	}

	protected virtual void Init(Type analysisRuleType, string originalLocation)
	{
		_analysisRuleType = analysisRuleType;
		_location = originalLocation;
		if (string.IsNullOrEmpty(_location))
		{
			_location = _analysisRuleType.Assembly.Location;
		}
	}

	internal override bool Compare(AnalysisRuleInfo value)
	{
		if (value is CodeAnalysisRuleInfoBase codeAnalysisRuleInfoBase)
		{
			if (_analysisRuleType != null && codeAnalysisRuleInfoBase._analysisRuleType != null && _analysisRuleType.FullName == codeAnalysisRuleInfoBase._analysisRuleType.FullName)
			{
				return base.Location == codeAnalysisRuleInfoBase.Location;
			}
			return false;
		}
		return false;
	}
}
