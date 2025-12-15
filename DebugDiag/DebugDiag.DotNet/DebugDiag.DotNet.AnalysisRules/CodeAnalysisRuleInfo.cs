using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace DebugDiag.DotNet.AnalysisRules;

[DataContract]
public class CodeAnalysisRuleInfo : CodeAnalysisRuleInfoBase
{
	private IAnalysisRuleBase _analysisRuleInstance;

	public IAnalysisRuleBase AnalysisRuleInstance
	{
		get
		{
			if (_analysisRuleInstance == null)
			{
				_analysisRuleInstance = (IAnalysisRuleBase)Activator.CreateInstance(_analysisRuleType);
			}
			return _analysisRuleInstance;
		}
	}

	public override string DisplayName => _analysisRuleType.Name;

	public override string Category
	{
		get
		{
			if (AnalysisRuleInstance is IAnalysisRuleMetadata analysisRuleMetadata)
			{
				return analysisRuleMetadata.Category;
			}
			return _analysisRuleType.Namespace;
		}
	}

	public override string Description
	{
		get
		{
			if (AnalysisRuleInstance is IAnalysisRuleMetadata analysisRuleMetadata)
			{
				return analysisRuleMetadata.Description;
			}
			return "";
		}
	}

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
			Init(assembly.GetType(name, throwOnError: true), null);
		}
	}

	public CodeAnalysisRuleInfo(Type analysisRuleType, string originalLocation)
		: base(analysisRuleType, originalLocation)
	{
	}

	internal override bool Compare(AnalysisRuleInfo value)
	{
		if (value is CodeAnalysisRuleInfo codeAnalysisRuleInfo)
		{
			if (_analysisRuleType != null && codeAnalysisRuleInfo._analysisRuleType != null && _analysisRuleType.FullName == codeAnalysisRuleInfo._analysisRuleType.FullName)
			{
				return base.Location == codeAnalysisRuleInfo.Location;
			}
			return false;
		}
		return false;
	}
}
