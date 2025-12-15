using System;
using System.Runtime.Serialization;
using System.Threading;

namespace DebugDiag.DotNet.AnalysisRules;

[DataContract]
[KnownType(typeof(MexCodeAnalysisRuleInfo))]
[KnownType(typeof(CodeAnalysisRuleInfo))]
[KnownType(typeof(XamlAnalysisRuleInfo))]
public abstract class AnalysisRuleInfo
{
	private AnalysisRuleStatus _status;

	private ManualResetEvent _completionEvent;

	private Exception _error;

	private string _failingDump;

	protected string _location;

	public bool WasFiltered;

	public string FilterReason;

	public string Location => _location;

	public AnalysisRuleStatus Status
	{
		get
		{
			return _status;
		}
		set
		{
			if (_status != value)
			{
				_status = value;
				if (value == AnalysisRuleStatus.Complete && _completionEvent != null)
				{
					_completionEvent.Set();
				}
			}
		}
	}

	public Exception Exception
	{
		get
		{
			return _error;
		}
		set
		{
			_error = value;
			Status = AnalysisRuleStatus.Complete;
		}
	}

	public string RecentDumpFilePath
	{
		get
		{
			return _failingDump;
		}
		set
		{
			_failingDump = value;
		}
	}

	public virtual string DisplayName
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual string Category
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual string Description
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	[DataMember]
	internal virtual string SerializedTypeInfo
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public bool WaitForCompletion(int millisecondsTimeout)
	{
		switch (_status)
		{
		case AnalysisRuleStatus.NotStarted:
			throw new Exception("Rule must be started before waiting");
		case AnalysisRuleStatus.Complete:
			return true;
		case AnalysisRuleStatus.CheckingFilter:
		case AnalysisRuleStatus.Running:
			_completionEvent = new ManualResetEvent(initialState: false);
			return _completionEvent.WaitOne(millisecondsTimeout);
		default:
			throw new Exception($"Unknown status: {_status.ToString()}");
		}
	}

	internal virtual RuleFilterResults GetRuleFilterResult(NetScriptManager manager)
	{
		throw new NotImplementedException();
	}

	internal virtual RuleFilterResults GetRuleFilterResult(NetDbgObj debugger)
	{
		throw new NotImplementedException();
	}

	internal virtual bool Compare(AnalysisRuleInfo value)
	{
		throw new NotImplementedException();
	}
}
