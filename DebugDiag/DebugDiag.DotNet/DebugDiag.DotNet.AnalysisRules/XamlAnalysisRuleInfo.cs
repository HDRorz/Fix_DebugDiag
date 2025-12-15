using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet.AnalysisRules;

[DataContract]
public class XamlAnalysisRuleInfo : AnalysisRuleInfo
{
	private string _name;

	private string _filterXamlPath;

	private IDictionary<string, object> _workflowOutputs;

	private const int FILTER_XAML_TIMEOUT = 60000;

	public string FilterXamlPath
	{
		get
		{
			if (_filterXamlPath == null)
			{
				_filterXamlPath = Path.ChangeExtension(base.Location, ".filter.xaml");
			}
			return _filterXamlPath;
		}
	}

	public override string DisplayName => _name;

	public override string Category => "Workflow-based Rules";

	public override string Description => "";

	[DataMember]
	internal override string SerializedTypeInfo
	{
		get
		{
			return base.Location;
		}
		set
		{
			Init(value);
		}
	}

	public XamlAnalysisRuleInfo(string xamlPath)
	{
		Init(xamlPath);
	}

	private void Init(string xamlPath)
	{
		if (string.IsNullOrEmpty(xamlPath))
		{
			throw new ArgumentNullException("xamlPath");
		}
		if (!File.Exists(xamlPath))
		{
			throw new Exception($"Could not load Xaml: {xamlPath}");
		}
		_location = xamlPath;
		_name = new FileInfo(base.Location).Name;
	}

	internal override RuleFilterResults GetRuleFilterResult(NetScriptManager manager)
	{
		return ExecuteWorkflowFilter("Manager", manager);
	}

	internal override RuleFilterResults GetRuleFilterResult(NetDbgObj debugger)
	{
		return ExecuteWorkflowFilter("Debugger", debugger);
	}

	private RuleFilterResults ExecuteWorkflowFilter(string filterParamName, object filterParam)
	{
		if (!File.Exists(FilterXamlPath))
		{
			return RuleFilterResults.NoFilter;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add(filterParamName, filterParam);
		base.Status = AnalysisRuleStatus.CheckingFilter;
		IDictionary<string, object> dictionary2 = ExecuteWorkflow(FilterXamlPath, 60000, dictionary);
		base.Status = AnalysisRuleStatus.NotStarted;
		if (dictionary2.ContainsKey("Result") && (bool)dictionary2["Result"])
		{
			return RuleFilterResults.FilterReturnedTrue;
		}
		return RuleFilterResults.FilterReturnedFalse;
	}

	internal void RunAnalysisRule(int timeoutInMiliseconds, NetScriptManager manager, List<string> dumpFiles)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string dumpFile in dumpFiles)
		{
			stringBuilder.AppendFormat("{0};", dumpFile);
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Manager", manager);
		RunWorkflowRule(stringBuilder.ToString(), timeoutInMiliseconds, dictionary);
	}

	internal void RunAnalysisRule(int timeoutInMiliseconds, NetScriptManager manager, NetDbgObj debugger)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Manager", manager);
		dictionary.Add("Debugger", debugger);
		RunWorkflowRule(debugger.DumpFileFullPath, timeoutInMiliseconds, dictionary);
	}

	internal void RunAnalysisRule(int timeoutInMiliseconds, NetScriptManager manager, NetDbgObj debugger, NetDbgThread thread)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Manager", manager);
		dictionary.Add("Debugger", debugger);
		dictionary.Add("Thread", thread);
		RunWorkflowRule(debugger.DumpFileFullPath, timeoutInMiliseconds, dictionary);
	}

	internal void RunAnalysisRule(int timeoutInMiliseconds, NetScriptManager manager, NetDbgObj debugger, NetDbgThread thread, NetDbgException nativeException, ClrException managedException)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Manager", manager);
		dictionary.Add("Debugger", debugger);
		dictionary.Add("ExceptionThread", thread);
		dictionary.Add("NativeException", nativeException);
		dictionary.Add("ManagedException", managedException);
		RunWorkflowRule(debugger.DumpFileFullPath, timeoutInMiliseconds, dictionary);
	}

	private void RunWorkflowRule(string recentDumpFilePath, int timeoutInMiliseconds, Dictionary<string, object> workflowParams)
	{
		base.RecentDumpFilePath = recentDumpFilePath;
		base.Status = AnalysisRuleStatus.Running;
		ExecuteWorkflow(base.Location, timeoutInMiliseconds, workflowParams);
	}

	private IDictionary<string, object> ExecuteWorkflow(string xamlPath, int timeoutInMiliseconds, Dictionary<string, object> workflowParams)
	{
		WorkflowApplication workflowApplication = new WorkflowApplication((DynamicActivity)ActivityXamlServices.Load(xamlPath), workflowParams);
		workflowApplication.Completed = WorkflowCompleted;
		workflowApplication.OnUnhandledException = WorkflowUnhandledException;
		workflowApplication.SynchronizationContext = SynchronizationContext.Current;
		if (workflowParams.ContainsKey("Manager"))
		{
			workflowApplication.Extensions.Add(workflowParams["Manager"]);
		}
		workflowApplication.Run();
		Application.DoEvents();
		if (WaitForCompletion(timeoutInMiliseconds))
		{
			return _workflowOutputs;
		}
		workflowApplication.Abort("Timeout - " + TimeSpan.FromMilliseconds(timeoutInMiliseconds).ToString());
		return null;
	}

	private UnhandledExceptionAction WorkflowUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs arg)
	{
		base.Exception = arg.UnhandledException;
		return UnhandledExceptionAction.Abort;
	}

	private void WorkflowCompleted(WorkflowApplicationCompletedEventArgs obj)
	{
		_workflowOutputs = obj.Outputs;
		base.Status = AnalysisRuleStatus.Complete;
	}

	internal override bool Compare(AnalysisRuleInfo value)
	{
		if (value is XamlAnalysisRuleInfo xamlAnalysisRuleInfo)
		{
			return base.Location == xamlAnalysisRuleInfo.Location;
		}
		return false;
	}
}
